﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TheManager
{
    [DataContract(IsReference =true)]
    public class Club_Ville : Club
    {

        [DataMember]
        private int _budget;
        [DataMember]
        private Ville _ville;
        [DataMember]
        private float _sponsor;
        [DataMember]
        private List<Contrat> _joueurs;
        [DataMember]
        private HistoriqueClub _historique;

        public int Budget { get => _budget; }
        public Ville Ville { get => _ville; }
        public float Sponsor { get => _sponsor; }
        public List<Contrat> Contrats { get => _joueurs; }
        public HistoriqueClub Historique { get => _historique; }

        public float MasseSalariale
        {
            get
            {
                float res = 0;

                foreach (Contrat c in _joueurs) res += c.Salaire;

                return res;
            }
        }

        public Club_Ville(string nom, string nomCourt, int reputation, int budget, int supporters, int centreFormation, Ville ville, string logo, Stade stade) : base(nom,nomCourt,reputation,supporters,centreFormation,logo,stade)
        {
            _budget = budget;
            _ville = ville;
            _sponsor = 0;
            _joueurs = new List<Contrat>();
            _historique = new HistoriqueClub();
        }

        public void AjouterJoueur(Contrat c)
        {
            _joueurs.Add(c);
        }

        public void RetirerJoueur(Joueur j)
        {
            Contrat aRetirer = null;

            foreach (Contrat ct in _joueurs) if (ct.Joueur == j) aRetirer = ct;

            if (aRetirer != null)
                _joueurs.Remove(aRetirer);
        }

        public override List<Joueur> Joueurs()
        {
            List<Joueur> joueurs = new List<Joueur>();
            foreach (Contrat c in _joueurs)
                joueurs.Add(c.Joueur);
            return joueurs;
        }

        public override float Niveau()
        {
            float niveau = 0;
            foreach(Contrat c in _joueurs)
            {
                niveau += c.Joueur.Niveau;
            }
            return niveau / (_joueurs.Count+0.0f);
        }

        public void GenererJoueur(Poste p, int ageMin, int ageMax)
        {
            string prenom = _ville.Pays().Langue.ObtenirPrenom();
            string nom = _ville.Pays().Langue.ObtenirNom();
            int anneeNaissance = Session.Instance.Random(Session.Instance.Partie.Date.Year - ageMax, Session.Instance.Partie.Date.Year - ageMin+1);

            //Potentiel
            int potentiel = Session.Instance.Random(CentreFormation - 18, CentreFormation + 18);
            if (potentiel < 1) potentiel = 1;
            if (potentiel > 99) potentiel = 99;

            //Niveau
            int age = Session.Instance.Partie.Date.Year - anneeNaissance;
            int diff = 24 - age;
            int niveau = potentiel;
            if (diff > 0) niveau -= 3 * diff;
            
            Joueur j = new Joueur(prenom, nom, new DateTime(anneeNaissance, Session.Instance.Random(1,13), Session.Instance.Random(1,29)), niveau, potentiel, this.Ville.Pays(), p);
            int annee = Session.Instance.Random(Session.Instance.Partie.Date.Year + 1, Session.Instance.Partie.Date.Year + 5);
            Contrats.Add(new Contrat(j, j.EstimerSalaire(), new DateTime(annee, 7, 1)));
        }

        public void GenererJoueur(int ageMin, int ageMax)
        {
            Poste p = Poste.GARDIEN;
            int random = Session.Instance.Random(1, 12);
            if(random >= 2 && random <= 5) p = Poste.DEFENSEUR;
            if (random >= 6 && random <= 9) p = Poste.MILIEU;
            if (random >= 10) p = Poste.ATTAQUANT;
            GenererJoueur(p,ageMin,ageMax);
        }

        public void ModifierBudget(int somme)
        {
            _budget += somme;
        }
        
        public void PayerSalaires()
        {
            foreach(Contrat ct in Contrats)
            {
                ModifierBudget(-ct.Salaire);
            }
        }

        public void SubvensionSponsor()
        {
            ModifierBudget((int)(Sponsor / 12));
        }

        public void ObtenirSponsor()
        {
            int sponsor = 0;
            float niveau = Niveau();
            if (niveau < 1000) sponsor = Session.Instance.Random(5000, 14000);
            else if (niveau < 3000) sponsor = Session.Instance.Random(85000, 323000);
            else if (niveau < 4000) sponsor = Session.Instance.Random(200000, 500000);
            else if (niveau < 5000) sponsor = Session.Instance.Random(500000, 800000);
            else if (niveau < 6000) sponsor = Session.Instance.Random(800000, 2500000);
            else if (niveau < 7000) sponsor = Session.Instance.Random(2500000, 6500000);
            else if (niveau < 8000) sponsor = Session.Instance.Random(6000000, 14000000);
            else if (niveau < 9000) sponsor = Session.Instance.Random(14000000, 23000000);
            else sponsor = Session.Instance.Random(23000000, 40000000);
            _sponsor = sponsor;
        }

        /// <summary>
        /// Fin d'année : mise à jour du centre de formation par le club
        /// Baisse de niveau randomly
        /// Si le club à de l'argent il peut le renflouer
        /// </summary>
        public void MiseAJourCentreFormation()
        {
            _centreFormation -= Session.Instance.Random(1, 3);
            if (_centreFormation < 1)
                _centreFormation = 1;
            for (int i = 0; i<5; i++)
            {
                int prix = (int)(967.50471* Math.Pow(1.12867,CentreFormation));
                //Console.WriteLine(Nom + " doit payer " + prix + " euros pour ameliorer son centre.");
                if (_budget/3 > prix && CentreFormation<99)
                {
                    ModifierBudget(-prix);
                    _centreFormation++;
                }
            }
        }

        /// <summary>
        /// Génère quelques juniors chaque début d'année pour le club
        /// </summary>
        public void GenererJeunes()
        {

            int nb = Session.Instance.Random(1, 3);

            int nbJoueursClub = Contrats.Count;
            if(nbJoueursClub < 13)
            {
                nb = 13 - nbJoueursClub;
            }
            for(int i = 0; i<nb; i++)
            {
                GenererJoueur(17, 19);
            }
        }

        /// <summary>
        /// Tente de prolonger un contrat
        /// </summary>
        /// <param name="ct">Le contrat à prolonger</param>
        /// <returns>Vrai si le contrat a été prolongé, faux sinon</returns>
        public bool Prolonger(Contrat ct)
        {
            bool res = false;
            int salaire = ct.Joueur.EstimerSalaire();
            if (salaire < ct.Salaire) salaire = ct.Salaire;
            salaire = (int)(salaire * (Session.Instance.Random(10, 14) / (10.0f)));

            bool ageValide = true;
            if (ct.Joueur.Age > 32)
                if (Session.Instance.Random(1, 3) == 1) ageValide = false;

            bool assezBon = true;
            if (ct.Joueur.Age < 25 && ct.Joueur.Potentiel < Niveau() - 12)
                assezBon = false;
            else if (ct.Joueur.Age >= 25 && ct.Joueur.Niveau < Niveau() - 12)
                assezBon = false;

            if(_budget > 12*salaire && ageValide && assezBon)
            {
                res = true;
                int annee = Session.Instance.Random(Session.Instance.Partie.Date.Year + 1, Session.Instance.Partie.Date.Year + 5);
                ct.MettreAJour(salaire, new DateTime(annee, 7, 1));
            }

            return res;
        }

        public void MettreAJourListeTransferts()
        {
            float niveauClub = Niveau();
            foreach(Contrat ct in _joueurs)
            {
                //Si le joueur est trop mauvais
                if (ct.Joueur.Potentiel / niveauClub < 0.80) ct.Transferable = true;
                else ct.Transferable = false;
            }
        }

        public void RecevoirOffre(Contrat contrat, Club_Ville interessee, int somme, int salaire, int dureeContrat)
        {
            if(contrat.Transferable)
            {
                if(somme < contrat.Joueur.EstimerValeurTransfert())
                {
                    contrat.Joueur.Offres.Add(new OffreContrat(interessee, salaire, dureeContrat));
                }
            }
            else
            {
                if(somme > contrat.Joueur.EstimerValeurTransfert()*1.2f)
                {
                    contrat.Joueur.Offres.Add(new OffreContrat(interessee, salaire, dureeContrat));
                }
            }
        }

        public void RechercherJoueursLibres()
        {
            float niveau = Niveau();
            Pays paysClub = Ville.Pays();

            int chance = 100 - (int)niveau;

            int joueursARechercher = 20 - Contrats.Count;
            if (joueursARechercher < 0) joueursARechercher = 0;

            int i = 0;
            bool poursuivre = true;
            int joueursTrouves = 0;
            while(poursuivre)
            {
                Joueur j = Session.Instance.Partie.Gestionnaire.JoueursLibres[i];
                //Susceptible d'interesser le club
                if ((Session.Instance.Random(1,chance) == 1) && j.Niveau / niveau > 0.90f)
                {
                    bool peut = true;
                    //Si le joueur n'a pas un niveau pro, il faut qu'il soit du même pays
                    if (j.Niveau < 60 && j.Nationalite != paysClub) peut = false;
                    if(peut)
                    {
                        int salaire = (int)(j.EstimerSalaire() * (Session.Instance.Random(80, 120) / 100.0f));
                        j.Offres.Add(new OffreContrat(this, salaire, Session.Instance.Random(1, 5)));
                        joueursTrouves++;
                        //Console.WriteLine(Nom + " emet une offre vers " + j);
                    }
                }
                i++;
                if (i == Session.Instance.Partie.Gestionnaire.JoueursLibres.Count || joueursTrouves == joueursARechercher)
                    poursuivre = false;
            }
        }

    }
}