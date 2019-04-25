﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TheManager
{
    [DataContract(IsReference =true)]
    public class Journaliste
    {

        [DataMember]
        private string _prenom;
        [DataMember]
        private string _nom;
        [DataMember]
        private Ville _base;
        [DataMember]
        private int _retrait;

        public string Prenom { get => _prenom; }
        public string Nom { get => _nom; }
        [DataMember]
        public int Age { get; set; }
        [DataMember]
        public bool EstPris { get; set; }
        public Ville Base { get => _base; }
        public int Retrait { get => _retrait; }

        public Media Media
        {
            get
            {
                Media res = null;
                foreach(Media m in Session.Instance.Partie.Gestionnaire.Medias)
                {
                    foreach (Journaliste j in m.Journalistes) if (j == this) res = m;
                }
                return res;
            }
        }

        public Journaliste(string prenom, string nom, int age, Ville _base, int retrait)
        {
            EstPris = false;
            _prenom = prenom;
            _nom = nom;
            Age = age;
            this._base = _base;
            _retrait = retrait;
        }

        public override string ToString()
        {
            return Prenom + " " + Nom;
        }
    }
}