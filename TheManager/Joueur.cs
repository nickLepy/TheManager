﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TheManager
{
    [DataContract]
    public struct HistoriqueJoueur : IEquatable<HistoriqueJoueur>
    {
        [DataMember]
        public int Niveau { get; set; }
        [DataMember]
        public int Annee { get; set; }
        [DataMember]
        public int Buts { get; set; }
        [DataMember]
        public int MatchsJoues { get; set; }
        [DataMember]
        public CityClub Club { get; set; }
        public HistoriqueJoueur(int niveau, int annee, int buts, int matchJoues, CityClub club)
        {
            Niveau = niveau;
            Annee = annee;
            Buts = buts;
            MatchsJoues = matchJoues;
            Club = club;
        }
        
        public bool Equals(HistoriqueJoueur other)
        {
            return (Annee > other.Annee);
        }
    }

    [DataContract(IsReference =true)]
    public class Joueur : Personne
    {
        [DataMember]
        private int _niveau;
        [DataMember]
        private int _potentiel;
        [DataMember]
        private Position _poste;
        [DataMember]
        private bool _suspendu;
        [DataMember]
        private int _energie;
        [DataMember]
        private List<HistoriqueJoueur> _historique;
        [DataMember]
        private List<ContractOffer> _offres;

        public int Niveau { get => _niveau; set => _niveau = value; }
        public int Potentiel { get => _potentiel; }
        public Position Poste { get => _poste;}
        public bool Suspendu { get => _suspendu; set => _suspendu = value; }
        public List<HistoriqueJoueur> Historique { get => _historique; }
        /// <summary>
        /// Matchs joués sur la saison en cours
        /// </summary>
        [DataMember]
        public int MatchsJoues { get; set; }
        /// <summary>
        /// Buts marqués sur la saison en cours
        /// </summary>
        [DataMember]
        public int ButsMarques { get; set; }
        public int Energie
        {
            get { return _energie; }
            set
            {
                _energie = value;
                if (_energie > 100) _energie = 100;
                if (_energie < 0) _energie = 0;
            }
        }

        /// <summary>
        /// Club actuel du joueur
        /// </summary>
        public CityClub Club
        {
            get
            {
                CityClub res = null;
                foreach(Club c in Session.Instance.Partie.Gestionnaire.Clubs)
                {
                    CityClub cv = c as CityClub;
                    if(cv != null)
                    {
                        foreach(Contrat ct in cv.contracts)
                        {
                            if (ct.Joueur == this)
                                res = cv;
                        }
                    }
                }
                return res;
            }
        }

        public Joueur(string prenom, string nom, DateTime naissance, int niveau, int potentiel, Pays nationalite, Position poste) : base(prenom,nom,naissance,nationalite)
        {
            
            _niveau = niveau;
            _potentiel = potentiel;
            _poste = poste;
            Suspendu = false;
            _energie = 100;
            _historique = new List<HistoriqueJoueur>();
            ButsMarques = 0;
            MatchsJoues = 0;
            _offres = new List<ContractOffer>();

        }

        

        //Propriétés
        //nb de jaunes cette saison
        //nb de rouges cette saison
        //nb de buts cette saison
        //appelé en sélection

        public void Recuperer()
        {
            if(Energie < 100)
                Energie += Session.Instance.Random(2, 6);
        }

        public int EstimerSalaire()
        {
            int salaire = (int)(0.292188*Math.Pow(1.1859960,Niveau));
            switch (_poste)
            {
                case Position.Goalkeeper:
                    salaire = (int)(salaire *0.8f);
                    break;
                case Position.Defender:
                    salaire = (int)(salaire * 0.9f);
                    break;
                case Position.Striker:
                    salaire = (int)(salaire * 1.1f);
                    break;
            }
            return salaire;
        }

        public int EstimerValeurTransfert()
        {
            return EstimerSalaire() * 100;
        }

        /// <summary>
        /// Met à jour le niveau du joueur, et enregistre le niveau actuel dans l'historique
        /// </summary>
        public void MiseAJourNiveau()
        {
            int age = Age;

            if(age < 24)
            {
                _niveau += Session.Instance.Random(1, 5);
            }

            if(age > 29)
            {
                _niveau -= Session.Instance.Random(1, 5);
            }
            _historique.Add(new HistoriqueJoueur(_niveau, Session.Instance.Partie.Date.Year + 1,ButsMarques, MatchsJoues, Club));
            ButsMarques = 0;
            MatchsJoues = 0;
        }


        /// <summary>
        /// Considère une offre de contrat
        /// </summary>
        /// <param name="oc">L'offre à considérer</param>
        /// <returns>Vrai si l'offre à été acceptée par le joueur, faux sinon</returns>
        public bool ConsidererOffre(ContractOffer oc, CityClub porteur)
        {
            bool res = false;
            //Si le joueur à un club
            if (Club != null)
            {
                if (porteur.Level() - Club.Level() > Session.Instance.Random(-10, -5))
                {
                    Contrat sonContrat = null;
                    foreach (Contrat ct in Club.contracts) if (ct.Joueur == this) sonContrat = ct;
                    //Si le salaire proposé est en légère augmentation par rapport à son budget actuel
                    if ((oc.Wage + 0.0f) / sonContrat.Salaire > (Session.Instance.Random(100, 120) / 100.0f))
                    {
                        Club ancien = Club;
                        Club.RemovePlayer(this);
                        porteur.AddPlayer(new Contrat(this, oc.Wage, new DateTime(Session.Instance.Partie.Date.Year + oc.ContractDuration, 7, 1), new DateTime(Session.Instance.Partie.Date.Year, Session.Instance.Partie.Date.Month, Session.Instance.Partie.Date.Day)));
                        res = true;
                    }
                }
            }
            else
            {
                //Si l'offre du salaire n'est pas trop mauvaise (au moins entre 0.7 et 1 de sa "vrai valeur salariale")
                if ((oc.Wage + 0.0f) / EstimerSalaire() > (Session.Instance.Random(70, 100) / 100.0f))
                {
                    porteur.AddPlayer(new Contrat(this, oc.Wage, new DateTime(Session.Instance.Partie.Date.Year + oc.ContractDuration, 7, 1), new DateTime(Session.Instance.Partie.Date.Year, Session.Instance.Partie.Date.Month, Session.Instance.Partie.Date.Day)));
                    res = true;
                }
            }
            return res;
        }

        /*
        /// <summary>
        /// Le joueur considère toutes ses offres de contrats
        /// </summary>
        public void ConsidererOffres()
        {
            foreach(OffreContrat oc in _offres)
            {
                //Si le joueur à un club
                if(Club != null)
                {
                    if (oc.Club.Niveau() - Club.Niveau() > Session.Instance.Random(-10,-5))
                    {
                        Contrat sonContrat = null;
                        foreach (Contrat ct in Club.Contrats) if (ct.Joueur == this) sonContrat = ct;
                        //Si le salaire proposé est en légère augmentation par rapport à son budget actuel
                        if((oc.Salaire+0.0f) / sonContrat.Salaire > (Session.Instance.Random(100, 120) / 100.0f))
                        {
                            Club ancien = Club;
                            Club.RetirerJoueur(this);
                            oc.Club.AjouterJoueur(new Contrat(this, oc.Salaire, new DateTime(Session.Instance.Partie.Date.Year+oc.DureeContrat,7,1)));
                        }

                    }
                }
                else
                {
                    //Si l'offre du salaire n'est pas trop mauvaise (au moins entre 0.7 et 1 de sa "vrai valeur salariale")
                    if((oc.Salaire+0.0f) / EstimerSalaire() > (Session.Instance.Random(70,100)/100.0f))
                    {
                        oc.Club.AjouterJoueur(new Contrat(this, oc.Salaire, new DateTime(Session.Instance.Partie.Date.Year + oc.DureeContrat, 7, 1)));
                    }
                }
            }
            _offres.Clear();
        }*/
        
        public override string ToString()
        {
            return base.ToString();
        }

    }
}