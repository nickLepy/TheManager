﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TheManager
{

    public class ProgrammationTour
    {
        private Heure _heureParDefaut;
        private List<DateTime> _joursDeMatchs;
        private List<DecalagesTV> _decalagesTV;

        public Heure HeureParDefaut { get => _heureParDefaut; }
        public List<DateTime> JoursDeMatchs { get => _joursDeMatchs; }
        public List<DecalagesTV> DecalagesTV { get => _decalagesTV; }

        public ProgrammationTour(Heure heure, List<DateTime> jours, List<DecalagesTV> decalages)
        {
            _heureParDefaut = heure;
            _joursDeMatchs = new List<DateTime>(jours);
            _decalagesTV = decalages;
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

    public abstract class Tour
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

        public string Nom { get => _nom; }
        public List<Club> Clubs { get => _clubs; }
        public List<Match> Matchs { get => _matchs; }
        public bool AllerRetour { get => _allerRetour; }
        public ProgrammationTour Programmation { get => _programmation; }

        public Tour(string nom, Heure heure, List<DateTime> dates, List<DecalagesTV> decalages, bool allerRetour)
        {
            _nom = nom;
            _clubs = new List<Club>();
            _matchs = new List<Match>();
            _programmation = new ProgrammationTour(heure, dates, decalages);
            _allerRetour = allerRetour;
        }

        public abstract void Initialiser();
        public abstract List<Club> Qualifies();
    }
}