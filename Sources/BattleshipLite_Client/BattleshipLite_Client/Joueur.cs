﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipLite_Client
{
    public class Joueur
    {
        /// <summary>
        /// Le nom du joueur
        /// </summary>
        public string Nom { get; set; }
        /// <summary>
        /// Le plateau du joueur
        /// </summary>
        public Plateau Plateau { get; set; }
        /// <summary>
        /// La liste des coups joués par le joueur
        /// </summary>
        public List<Coup> Coups { get; set; }

        /// <summary>
        /// Constructeur de la classe Joueur
        /// </summary>
        /// <param name="nom">Nom du joueur</param>
        /// <param name="plateau">Plateau du joueur</param>
        public Joueur(string nom, Plateau plateau)
        {
            Nom = nom;
            Plateau = plateau;
            Coups = new List<Coup>();
        }

        /// <summary>
        /// Joue un coup
        /// </summary>
        /// <param name="joueur">Le joueur</param>
        /// <param name="_case">La case touchée par le joueur</param>
        public bool JouerCoup(Connexion connexion, Plateau plateau, string Coup)
        {

            Partie.ConvertToGrid(Coup, out int x, out int y);
            //Vérifie si coup valide
            if (!IsPlacementValide(x, y))
            {
                Console.WriteLine("Le coup est hors du plateau.");
                return false;
            }

            // Vérifie si la case a déjà été touchée
            Coup coupClient = new() { Case = new(x, y) };
            if (Coups.Any(c => c.Case.X == coupClient.Case.X && c.Case.Y == coupClient.Case.Y))
            {
                Console.WriteLine("La case a déjà été touchée.");
                return false;
            }


            // Envoi du coup au serveur
            connexion.Envoi(connexion._sender, JsonSerializer.Serialize<Coup>(coupClient));

            // Réception de la réponse du serveur
            string json = connexion.Recois(connexion._sender);
            Coup attaque = JsonSerializer.Deserialize<Coup>(json);

            Coups.Add(coupClient);
            plateau.Grille[coupClient.Case.X][coupClient.Case.Y].ToucheCase();


            if (attaque.EstReussi)
            {
                coupClient.EstReussi = true;

                foreach (Bateau bateau in plateau.Bateaux)
                {

                    Case caseTouchee = bateau.Positions.FirstOrDefault(_case => _case.X == coupClient.Case.X && _case.Y == coupClient.Case.Y);
                    if (caseTouchee != null)
                    {
                        caseTouchee.ToucheCase(); //Pour bateau
                        Console.WriteLine("Le coup a touché l'ennemi !");

                    }

                }
            }
            else
            {
                Console.WriteLine("Le coup a échoué.");

            }
            return true;
        }
        public void VerifCoup(Connexion connexion, Plateau monPlateau, List<Coup> CoupsEnnemi)

        {

            // Réception du coup du serveur
            string json = connexion.Recois(connexion._sender);
            Coup coupServeur = JsonSerializer.Deserialize<Coup>(json);
            monPlateau.Grille[coupServeur.Case.X][coupServeur.Case.Y].ToucheCase();

            foreach (Bateau bateau in monPlateau.Bateaux)
            {
                Case caseTouchee = bateau.Positions.FirstOrDefault(_case => _case.X == coupServeur.Case.X && _case.Y == coupServeur.Case.Y);
                if (caseTouchee != null)
                {
                    caseTouchee.ToucheCase();
                    Console.WriteLine("L'ennemi à touché votre bateau.");
                    coupServeur.EstReussi = true;
                    break;
                }
                else
                {
                    Console.WriteLine("L'ennemi à tiré dans l'eau");
                }

            }

            CoupsEnnemi.Add(coupServeur);
            // Envoi de la réponse au serveur
            connexion.Envoi(connexion._sender, JsonSerializer.Serialize<Coup>(coupServeur));
        }
        /// <summary>
        /// Place le bateau horizontalement ou verticalement sur le plateau
        /// </summary>
        public bool PlacerBateau(Bateau bateau, string case1, string case2)
        {
            Partie.ConvertToGrid(case1, out int x1, out int y1);
            Case _case1 = new Case(x1, y1);

            Partie.ConvertToGrid(case2, out int x2, out int y2);
            Case _case2 = new Case(x2, y2);
            // Vérifier que les deux cases sont valides (dans les limites du plateau)
            if (IsPlacementValide(x1, y1) || IsPlacementValide(x2, y2))
            {
                // Vérifier que les deux cases sont adjacentes soit horizontalement, soit verticalement
                bool sontAdjacentes = (x1 == x2 && Math.Abs(y1 - y2) == 1) || (y1 == y2 && Math.Abs(x1 - x2) == 1);
                //Pas la même case
                bool pasPareil = (_case2 != _case1);
                bool surAutreBateau = false;
                foreach (Bateau bat in Plateau.Bateaux)
                {
                    foreach (Case c in bat.Positions)
                    {
                        if (c.X == _case1.X || c.Y == _case1.Y ||
                        (c.X == _case2.X && c.Y == _case2.Y))
                        {
                            surAutreBateau = true;
                            break;
                        }
                    }
                } 

                if (sontAdjacentes && pasPareil && !surAutreBateau)
                {
                    List<Case> positionBateau = new List<Case>();
                    positionBateau.Add(_case1);
                    positionBateau.Add(_case2);

                    bateau.PlacerBateau(positionBateau);
                    Plateau.Bateaux.Add(bateau);

                    Console.WriteLine($"Bateau placé en {case1} et {case2}");
                    return true;
                }
                else
                {
                    Console.WriteLine("Le bateau ne peux pas être placé de cette manière sur le plateau.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Les coordonnées sont hors du plateau.");
                return false;
            }
        }
        /// <summary>
        /// Place le bateau en diagonal sur le plateau
        /// </summary>
        /// <param name="b"></param>
        /// <param name="case1"></param>
        /// <param name="case2"></param>
        /// <param name="case3"></param>
        /// <returns></returns>
        public bool PlacerBateau(Bateau b, string case1, string case2, string case3)
        {
            Partie.ConvertToGrid(case1, out int x1, out int y1);
            Case _case1 = new Case(x1, y1);
            Partie.ConvertToGrid(case2, out int x2, out int y2);
            Case _case2 = new Case(x2, y2);
            Partie.ConvertToGrid(case3, out int x3, out int y3);
            Case _case3 = new Case(x3, y3);

            if (!IsPlacementValide(x1, y1) || !IsPlacementValide(x2, y2) || !IsPlacementValide(x3, y3))
            {
                Console.WriteLine("Les coordonnées sont hors du plateau.");
                return false;
            }
            bool sontAdjacantesDescendentes = (_case1.X == _case2.X - 1 && _case1.Y == _case2.Y - 1 &&
                                    _case2.X == _case3.X - 1 && _case2.Y == _case3.Y - 1);

            bool sontAdjacantesMontantes = (_case1.X == _case2.X + 1 && _case1.Y == _case2.Y - 1 &&
                                                _case2.X == _case3.X + 1 && _case2.Y == _case3.Y - 1);

            bool pasPareil = (_case1 != _case2 && _case2 != _case3 && _case1 != _case3);
            bool surAutreBateau = false;
            foreach (Bateau bat in Plateau.Bateaux)
            {
                foreach(Case c in bat.Positions)
                {
                    if (c.X == _case1.X || c.Y == _case1.Y ||
                        (c.X == _case2.X && c.Y == _case2.Y) ||
                        (c.X == _case3.X && c.Y == _case3.Y))
                    {
                        surAutreBateau = true;
                        break;
                    }
                }
            }

            if (pasPareil && (sontAdjacantesDescendentes || sontAdjacantesMontantes) && !surAutreBateau)
            {
                List<Case> positionBateau = new List<Case>();
                positionBateau.Add(_case1);
                positionBateau.Add(_case2);
                positionBateau.Add(_case3);

                b.PlacerBateau(positionBateau);
                Plateau.Bateaux.Add(b);
                Console.WriteLine($"Bateau placé en {case1}, {case2} et {case3}");
                return true;
            }
            else
            {
                Console.WriteLine("Le bateau ne peux pas être placé de cette manière sur le plateau.");
                return false;
            }
        }
        public bool IsPlacementValide(int x, int y)
        {
            if (x >= 0 && x < Plateau.Hauteur && y >= 0 && y < Plateau.Largeur)
            {
                return true;
            }
            return false;
        }

    }
}
