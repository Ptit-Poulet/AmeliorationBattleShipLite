﻿using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BattleshipLite_Serveur
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Connexion
            int port = 0;
            Console.WriteLine("Veuillez entrer le port:");
            bool valide = int.TryParse(Console.ReadLine(), out port);
            while (!valide)
            {
                Console.WriteLine("Veuillez entrer le port:");
                valide = int.TryParse(Console.ReadLine(), out port);
            }
            while (true)
            {

                Connexion connexion = new Connexion(port);
                connexion.StarterServeur();


                while (connexion._handler.Connected)
                {

                    //Début partie
                    Partie partie = new();
                    bool confirmation = false;
                    int hauteur = 0, largeur = 0;


                    do
                    {
                        Console.WriteLine("Entrez les dimensions du plateau de jeu.\n");
                        int GetDimension(string dimensionName)
                        {
                            int dimension;
                            do
                            {
                                Console.WriteLine($"Veuillez entrer la {dimensionName} (2 à 26) :");
                            } while (!int.TryParse(Console.ReadLine(), out dimension) || dimension < 2 || dimension > 26);
                            return dimension;
                        }

                        hauteur = GetDimension("hauteur");
                        largeur = GetDimension("largeur");

                        Console.WriteLine($"Envoi d'une validation des dimensions au client.");
                        // Envoyer les dimensions du serveur au client
                        if (!connexion.Envoi(connexion._handler, $"Est-ce qu'un plateau de {hauteur} par {largeur} vous convient ? [O/N]"))
                        {
                            break;
                        }
                        string reponse = connexion.Recois(connexion._handler);
                        reponse = reponse.Remove(1, 2);

                        if (reponse.ToUpper() == "O")
                        {
                            confirmation = true;
                        }

                        if (reponse.ToUpper() == "N")
                        {
                            Console.WriteLine($"Le client n'accepte pas ces dimensions.");

                        }

                    } while (!confirmation);
                    Console.WriteLine("Les dimensions ont été confirmé !\n");

                    // Démarrer la partie 
                    partie.Demarrer(ref partie, hauteur, largeur);

                    //TODO: #3 avoir 3 bateaux
                    //Placement bateau
                    Bateau bateau = new("Kayak", new List<Case>());

                    Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);
                    bool estPlace = false;
                    string devant, derriere;

                    do
                    {
                        Console.WriteLine("Veuillez placer le devant de votre bateau: ");
                        devant = Console.ReadLine();
                        Console.WriteLine("Veuillez placer le derrière de votre bateau: ");
                        derriere = Console.ReadLine();

                        if (Partie.IsValidCoordinate(devant) && Partie.IsValidCoordinate(derriere))
                        {
                            estPlace = partie.Joueurs[0].PlacerBateauEnI(bateau, devant, derriere);
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

                    // Envoyer le plateau du serveur au client
                    //Le break permet de détecter que le client s'est déconnecté
                    if (!connexion.Envoi(connexion._handler, JsonSerializer.Serialize(partie.Joueurs[0].Plateau)))
                    {
                        break;
                    }

                    Console.WriteLine("Bateau placé. Attente du client...");

                    // Réception du plateau du client
                    string json = connexion.Recois(connexion._handler);
                    //Le break permet de détecter que le client s'est déconnecté
                    if (json == String.Empty || json == null)
                    {
                        break;
                    }
                    Plateau plateauEnnemi = JsonSerializer.Deserialize<Plateau>(json);
                    partie.Joueurs[1].Plateau = plateauEnnemi;

                    Console.WriteLine("L'ennemi a placé son bateau. A l'attaque !");

                    Console.Clear();
                    Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);

                    // Jeu
                    Joueur? winner;
                    while (partie.EnCours)
                    {

                        if (!partie.CheckIfWinner(partie, out winner))
                        {

                            bool coupValide = false;
                            string coup;
                            Coup ContinueTour = new();

                            //Envoi coup
                            Affichage.PrintPlateauEnemi(partie.Joueurs[1].Plateau);
                            if (!partie.CheckIfWinner(partie, out winner))
                            {

                                do
                                {

                                    do
                                    {
                                        Affichage.PrintLegende();
                                        Console.WriteLine("Jouer votre coup: ");
                                        coup = Console.ReadLine();

                                        coupValide = Partie.IsValidCoordinate(coup) && partie.Joueurs[0].JouerCoup(connexion, partie.Joueurs[1].Plateau, coup);


                                        if (!coupValide)
                                        {
                                            Console.WriteLine("Coup invalide.");
                                        }

                                    } while (!coupValide);

                                    Affichage.PrintPlateauEnemi(partie.Joueurs[1].Plateau);
                                    ContinueTour = partie.Joueurs[0].Coups.Last();

                                    if (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner))
                                    {
                                        Console.WriteLine("C'est encore votre tour.");
                                    }

                                } while (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner));

                                if (!partie.CheckIfWinner(partie, out winner))
                                {

                                    do
                                    {

                                        //Recois coup client
                                        Console.WriteLine("Au tour du client.");
                                        partie.Joueurs[0].VerifCoup(connexion, partie.Joueurs[0].Plateau, partie.Joueurs[1].Coups);
                                        Affichage.PrintMonPlateau(partie.Joueurs[0].Plateau);

                                        ContinueTour = partie.Joueurs[1].Coups.Last();

                                        if (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner))
                                        {
                                            Console.WriteLine("C'est encore le tour du client.");
                                        }
                                    } while (ContinueTour.EstReussi && !partie.CheckIfWinner(partie, out winner));

                                }
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
                        Console.WriteLine("Demande d'un remath envoyé au client...");
                        string rematch = "Faire un rematch ? [O/N]\n";
                        if (!connexion.Envoi(connexion._handler, rematch))
                        {
                            break;
                        }
                        string reponse = connexion.Recois(connexion._handler);

                        //Le break permet de détecter que le client s'est déconnecté
                        if (reponse == String.Empty || reponse == null || !reponse.ToUpper().Contains("O"))
                        {
                            break;
                        }
                        Console.WriteLine("Rematch !");
                    }

                }
                Console.WriteLine("Connexion coupé, en attente d'un autre client...");
                connexion.ArreterServeur();

            }
        }
    }
}

