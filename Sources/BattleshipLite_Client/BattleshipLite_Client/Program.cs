using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BattleshipLite_Client
{
    internal class Program
    {
        static void Main(string[] args)

        {
            //Connexion
            string adresse, port;
            string reponse = "";
            Connexion conn = new Connexion();

            Console.WriteLine("Veuillez entrer l'adresse IP:");
            adresse = Console.ReadLine();
            Console.WriteLine("Veuillez entrer le port d'écoute:");
            port = Console.ReadLine();

            conn.StartClient(adresse, port, out bool estConnect);

            while (!estConnect)
            {
                Console.WriteLine("Veuillez entrer l'adresse IP:");
                adresse = Console.ReadLine();
                Console.WriteLine("Veuillez entrer le port d'écoute:");
                port = Console.ReadLine();

                conn.StartClient(adresse, port, out estConnect);
            }

            while (reponse.ToUpper() != "N")
            {

                //Début partie
                Thread.Sleep(1000);
                Console.WriteLine("le serveur défini les dimensions du plateau de jeu...");

                Partie partie = new();

                string confirmation = "";
                do
                {
                    string dimension = conn.Recois(conn._sender);
                    do
                    {
                        Console.WriteLine(dimension);
                        confirmation = Console.ReadLine();

                    }
                    while (confirmation.ToUpper() != "O" && confirmation.ToUpper() != "N");
                    if (confirmation.ToUpper() == "O")
                    {
                        Console.WriteLine("Dimensions confirmés !\n");
                    }

                    conn.Envoi(conn._sender, confirmation);

                } while (confirmation.ToUpper() == "N");

                Console.WriteLine("\nLe serveur place son bateau...");

                // Réception du plateau du serveur
                string json = conn.Recois(conn._sender);
                Plateau pEnnemi = JsonSerializer.Deserialize<Plateau>(json);
                partie.Demarrer(ref partie, pEnnemi.Hauteur, pEnnemi.Largeur);
                if(partie.Joueurs[1].Plateau.Bateaux.Count == 1)
                {
                Console.WriteLine("L'ennemi a placé son bateau, à votre tour.");
                }
                else
                {
                    Console.WriteLine("L'ennemi a placé ses bateaux, à votre tour.");

                }

                partie.Joueurs[1].Plateau = pEnnemi;

                // Placement du bateau 
                List<Bateau> Bateaux = new List<Bateau>();
                Bateau bateau = new("Torpilleur", new List<Case>());
                Bateaux.Add(bateau);
                int h = partie.Joueurs[1].Plateau.Hauteur,
                   l = partie.Joueurs[1].Plateau.Largeur;

                if (h > 6 && h < 27 && l > 6 && l < 27)
                {
                    Bateau bateau2 = new("Sous-marin", new List<Case>());
                    Bateaux.Add(bateau2);

                }
                if (h > 8 && h < 27 && l > 8 && l < 27)

                {
                    Bateau bateau3 = new("Porte-avions", new List<Case>());
                    Bateaux.Add(bateau3);

                }

                Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                string devant, milieu, derriere;
                for (int i = 0; i < Bateaux.Count; i++)
                {
                    bool estPlace = false;
                    Console.WriteLine($"Placement de votre {Bateaux[i].Nom}");
                    Affichage.PrintBateau(Bateaux[i]);
                    if (Bateaux[i].Nom == "Torpilleur")
                    {
                        do
                        {
                            Console.WriteLine("Veuillez placer le devant de votre bateau: ");
                            devant = Console.ReadLine();
                            Console.WriteLine("Veuillez placer le derrière de votre bateau: ");
                            derriere = Console.ReadLine();

                            if (Partie.IsValidCoordinate(devant) && Partie.IsValidCoordinate(derriere))
                            {
                                estPlace = partie.Joueurs[0].PlacerBateau(Bateaux[i], devant, derriere);
                                if (!estPlace)
                                {
                                    Console.WriteLine("Erreur de placement du bateau. Veuillez réessayer.");
                                    Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Coordonnées invalides.");
                            }

                        } while (!estPlace);

                        Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                    }

                    if (Bateaux[i].Nom == "Sous-marin")
                    {
                        do
                        {

                            Console.WriteLine("Veuillez placer le devant de votre bateau: ");
                            devant = Console.ReadLine();
                            Console.WriteLine("Veuillez placer le milieu de votre bateau: ");
                            milieu = Console.ReadLine();
                            Console.WriteLine("Veuillez placer le derrière de votre bateau: ");
                            derriere = Console.ReadLine();

                            if (Partie.IsValidCoordinate(devant) && Partie.IsValidCoordinate(milieu) && Partie.IsValidCoordinate(derriere))
                            {
                                estPlace = partie.Joueurs[0].PlacerBateau(Bateaux[i], devant, milieu, derriere);
                                if (!estPlace)
                                {
                                    Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                                }

                            }
                            else
                            {
                                Console.WriteLine("Coordonnées invalides.");
                            }
                        } while (!estPlace);

                        Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);

                    }

                    if (Bateaux[i].Nom == "Porte-avions")
                    {
                        do
                        {
                            Console.WriteLine("Veuillez placer le devant de votre bateau: ");
                            devant = Console.ReadLine();

                            if (Partie.IsValidCoordinate(devant))
                            {
                                estPlace = partie.Joueurs[0].PlacerBateau(Bateaux[i], devant);
                                if (!estPlace)
                                {
                                    Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                                }

                            }
                            else
                            {
                                Console.WriteLine("Coordonnées invalides.");
                            }
                        } while (!estPlace);
                        Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                    }
                }

                // Envoyer le plateau du client au serveur
                conn.Envoi(conn._sender, JsonSerializer.Serialize(partie.Joueurs[0].Plateau));
                Console.WriteLine("Bateau placé.");

                Console.Clear();
                Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);

                // Jeu
                Joueur? winner;
                while (partie.EnCours)
                {

                    if (!partie.CheckIfWinner(partie, out winner))
                    {
                        Coup ContinueTour = new();


                        do
                        {
                            //Recois coup serveur
                            Console.WriteLine("Au tour du serveur.");
                            partie.Joueurs[0].VerifCoup(conn, partie.Joueurs[0].Plateau, partie.Joueurs[1].Coups);

                            ContinueTour = partie.Joueurs[1].Coups.Last();

                            if (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner))
                            {
                                Console.WriteLine("C'est encore le tour du serveur.");
                            }
                            Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                        } while (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner));

                        if (!partie.CheckIfWinner(partie, out winner))
                        {
                            bool coupValide = false;
                            string coup;

                            //Envoi coup
                            Affichage.PrintPlateauEnnemi(partie.Joueurs[1].Plateau);

                            do
                            {

                                if (!partie.CheckIfWinner(partie, out winner))
                                {

                                    do
                                    {
                                        Affichage.PrintLegende();
                                        Console.WriteLine("Jouez votre coup: ");

                                        coup = Console.ReadLine();

                                        coupValide = Partie.IsValidCoordinate(coup) && partie.Joueurs[0].JouerCoup(conn, partie.Joueurs[1].Plateau, coup);
                                        if (!coupValide)
                                        {
                                            Console.WriteLine("Coup invalide.");
                                        }

                                    } while (!coupValide);
                                }

                                Affichage.PrintPlateauEnnemi(partie.Joueurs[1].Plateau);

                                ContinueTour = partie.Joueurs[0].Coups.Last();

                                if (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner))
                                {
                                    Console.WriteLine("C'est encore votre tour.");
                                }

                            } while (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner));
                        }
                    }
                    else
                    {

                        Console.Clear();
                        Affichage.MessageVictoire(winner, partie);
                        partie.EnCours = false;
                    }
                }

                if (!partie.EnCours)
                {

                    string rematch = conn.Recois(conn._sender);


                    do
                    {
                        Console.WriteLine(rematch);
                        reponse = Console.ReadLine();

                    }
                    while (reponse.ToUpper() != "O" && reponse.ToUpper() != "N");
                    if (reponse.ToUpper() == "O")
                    {
                        Console.WriteLine("Rematch !");
                    }

                    conn.Envoi(conn._sender, reponse);

                }
            }

        }
    }
}

