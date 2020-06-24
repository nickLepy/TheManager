﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheManager;
using TheManager.Comparators;

namespace TheManager_Console
{
    class Program
    {

        static void Main(string[] args)
        {
            Partie partie = new Partie();
            Session.Instance.Partie = partie;
            Gestionnaire g = partie.Gestionnaire;
            DatabaseLoader cbdd = new DatabaseLoader(g);
            cbdd.Load();

           
            for (int i = 0; i < 4840; i++)
                partie.Avancer();
            Console.WriteLine(partie.Date.ToShortDateString());
            
            Console.ReadLine();

        }
    }
}
