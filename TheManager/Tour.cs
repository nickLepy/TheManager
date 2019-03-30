﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheManager.Comparators;

namespace TheManager
{

    public struct RecuperationEquipes
    {
        public IEquipesRecuperables Source { get; set; }
        public int Nombre { get; set; }
        public RecuperationEquipes(IEquipesRecuperables source, int nombre)
        {
            Source = source;
            Nombre = nombre;
        }
    }


    public class ProgrammationTour
    {
        private Heure _heureParDefaut;
        private List<DateTime> _joursDeMatchs;
        private List<DecalagesTV> _decalagesTV;
        private DateTime _initialisation;
        private DateTime _fin;

        public Heure HeureParDefaut { get => _heureParDefaut; }
        public List<DateTime> JoursDeMatchs { get => _joursDeMatchs; }
        public List<DecalagesTV> DecalagesTV { get => _decalagesTV; }
        public DateTime Initialisation { get => _initialisation; }
        public DateTime Fin { get => _fin; }

        public ProgrammationTour(Heure heure, List<DateTime> jours, List<DecalagesTV> decalages, DateTime initialisation, DateTime fin)
        {
            _heureParDefaut = heure;
            _joursDeMatchs = new List<DateTime>(jours);
            _decalagesTV = decalages;
            _initialisation = initialisation;
            _fin = fin;
        }
    }

    public struct DecalagesTV
    {
        public int DecalageJours { get; set; }
        public Heure Heure { get; set; }
        
        public DecalagesTV(int nbjours, Heure heure)
        {
            DecalageJours = nbjours;
            Heure = heure;
        }
    }

    public struct Qualification
    {
        public int Classement { get; set; }
        public int IDTour { get; set; }
        public Competition Competition { get; set; }
        public bool AnneeSuivante { get; set; }

        public Qualification(int classement, int idtour, Competition competition, bool anneeSuivante)
        {
            Classement = classement;
            IDTour = idtour;
            Competition = competition;
            AnneeSuivante = anneeSuivante;
        }
    }

    public abstract class Tour : IEquipesRecuperables
    {
        /// <summary>
        /// Nom du tour
        /// </summary>
        protected string _nom;
        /// <summary>
        /// Liste des clubs participant à ce tour
        /// </summary>
        protected List<Club> _clubs;
        /// <summary>
        /// Liste des matchs du tour
        /// </summary>
        protected List<Match> _matchs;
        /// <summary>
        /// Si le tour se déroule en matchs aller-retour
        /// </summary>
        protected bool _allerRetour;

        /// <summary>
        /// Concerne la programmation générale des matchs du tour (TV, heure, jours)
        /// </summary>
        protected ProgrammationTour _programmation;

        protected List<Qualification> _qualifications;

        /// <summary>
        /// Liste des équipes récupérées d'autres compétitions en cours
        /// </summary>
        protected List<RecuperationEquipes> _recuperationsEquipes;



        public string Nom { get => _nom; }
        public List<Club> Clubs { get => _clubs; }
        public List<Match> Matchs { get => _matchs; }
        public bool AllerRetour { get => _allerRetour; }
        public ProgrammationTour Programmation { get => _programmation; }
        public List<Qualification> Qualifications { get => _qualifications; }
        public List<RecuperationEquipes> RecuperationEquipes { get => _recuperationsEquipes; }

        /*public Competition Competition
        {
            get
            {
                Competition competition = null;

                foreach(Competition c in Session.Instance.Partie.Gestionnaire.Competitions)
                {
                    foreach(Tour t in c.Tours)
                    {
                        if(t == this)
                        {
                            competition = c;
                        }
                    }
                }

                return competition;
            }
        }*/

        public Tour(string nom, Heure heure, List<DateTime> dates, List<DecalagesTV> decalages, DateTime initialisation, DateTime fin, bool allerRetour)
        {
            _nom = nom;
            _clubs = new List<Club>();
            _matchs = new List<Match>();
            _programmation = new ProgrammationTour(heure, dates, decalages, initialisation, fin);
            _allerRetour = allerRetour;
            _qualifications = new List<Qualification>();
            _recuperationsEquipes = new List<RecuperationEquipes>();
        }

        /// <summary>
        /// Joue les matchs du jour
        /// </summary>
        /// <returns>Vrai si au moins un match a été joué, faux sinon</returns>
        public bool JouerMatchs()
        {
            bool res = false;
            foreach (Match m in _matchs)
            {
                if (Session.Instance.Partie.Date.Date == m.Jour.Date)
                {
                    m.Jouer();
                }
            }

            return res;
        }

        public float MoyenneButs()
        {
            float res = 0;
            foreach(Match m in _matchs)
            {
                res += m.Score1 + m.Score2;
            }
            return res / ( _matchs.Count+0.0f);
        }

        /// <summary>
        /// Liste des buteurs par ordre décroissant
        /// </summary>
        /// <returns>Une liste de KeyValuePair avec le joueur en clé et son nombre de buts en valeur</returns>
        public List<KeyValuePair<Joueur, int>> Buteurs()
        {
            Dictionary<Joueur, int> buteurs = new Dictionary<Joueur, int>();
            foreach(Match m in _matchs)
            {
                foreach(EvenementMatch em in m.Evenements)
                {
                    if(em.Type == Evenement.BUT || em.Type == Evenement.BUT_PENALTY)
                    {
                        if (buteurs.ContainsKey(em.Joueur)) buteurs[em.Joueur]++;
                        else buteurs[em.Joueur] = 1;
                    }
                }
            }

            List<KeyValuePair<Joueur, int>> liste = buteurs.ToList();

            liste.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            return liste;
        }

        /// <summary>
        /// Renvoi la liste des prochains matchs à se jouer selon la date
        /// </summary>
        /// <returns></returns>
        public List<Match> ProchainsMatchs()
        {
            List<Match> res = new List<Match>();
            bool continuer = true;
            DateTime date = new DateTime(2000, 1, 1);
            int i = 0;
            if (_matchs.Count == 0) continuer = false;
            while (continuer)
            {
                Match m = _matchs[i];
                if (!m.Joue)
                {
                    if (date.Year == 2000)
                    {
                        date = m.Jour;
                        res.Add(m);
                    }
                    else if (date.Date == m.Jour.Date)
                        res.Add(m);
                    else continuer = false;
                }
                if (i == _matchs.Count - 1) continuer = false;
                i++;
            }


            return res;
        }

        public int Points(Club c)
        {
            int points = 0;
            foreach (Match m in _matchs)
            {
                if (m.Joue)
                {
                    if (m.Domicile == c)
                    {
                        if (m.Score1 > m.Score2)
                            points += 3;
                        else if (m.Score2 == m.Score1)
                            points++;
                    }
                    else if (m.Exterieur == c)
                    {
                        if (m.Score2 > m.Score1)
                            points += 3;
                        else if (m.Score2 == m.Score1)
                            points++;
                    }
                }
            }

            return points;
        }


        public int Joues(Club c)
        {
            int joues = 0;
            foreach (Match m in _matchs)
            {
                if (m.Joue && (m.Domicile == c || m.Exterieur == c)) joues++;
            }
            return joues;
        }

        public int Gagnes(Club c)
        {
            int res = 0;
            foreach (Match m in _matchs)
            {
                if (m.Domicile == c)
                {
                    if (m.Score1 > m.Score2) res++;
                }
                else if (m.Exterieur == c)
                {
                    if (m.Score2 > m.Score1) res++;
                }
            }
            return res;
        }

        public int Nuls(Club c)
        {
            int res = 0;
            foreach (Match m in _matchs)
            {
                if (m.Domicile == c || m.Exterieur == c)
                {
                    if (m.Score1 == m.Score2) res++;
                }
            }
            return res;
        }

        public int Perdus(Club c)
        {
            int res = 0;
            foreach (Match m in _matchs)
            {
                if (m.Domicile == c)
                {
                    if (m.Score1 < m.Score2) res++;
                }
                else if (m.Exterieur == c)
                {
                    if (m.Score2 < m.Score1) res++;
                }
            }
            return res;
        }

        public int ButsPour(Club c)
        {
            int res = 0;
            foreach (Match m in _matchs)
            {
                if (m.Domicile == c)
                {
                    res += m.Score1;
                }
                else if (m.Exterieur == c)
                {
                    res += m.Score2;
                }
            }
            return res;
        }

        public int ButsContre(Club c)
        {
            int res = 0;
            foreach (Match m in _matchs)
            {
                if (m.Domicile == c)
                {
                    res += m.Score2;
                }
                else if (m.Exterieur == c)
                {
                    res += m.Score1;
                }
            }
            return res;
        }

        public int Difference(Club c)
        {
            return ButsPour(c) - ButsContre(c);
        }

        public void RAZ()
        {
            _matchs = new List<Match>();
            _clubs = new List<Club>();
        }

        /// <summary>
        /// Ajoute les équipes à récupérer d'autres compétitions
        /// Ex : 32ème CDF -> L1
        /// Ex : Euro -> Equipes européennes
        /// </summary>
        public void AjouterEquipesARecuperer()
        {
            foreach(RecuperationEquipes re in _recuperationsEquipes)
            {
                foreach(Club c in re.Source.RecupererEquipes(re.Nombre))
                {
                    _clubs.Add(c);
                }
            }
        }

        public abstract void Initialiser();
        public abstract void QualifierClubs();
        public abstract Tour Copie();

        

        public List<Club> RecupererEquipes(int nombre)
        {
            List<Club> clubs = new List<Club>(_clubs);
            clubs.Sort(new Club_Niveau_Comparator());
            List<Club> res = new List<Club>();
            for (int i = 0; i < nombre; i++) res.Add(clubs[i]);
            return res;
        }
    }
}