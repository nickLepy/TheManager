﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

namespace TheManager
{
    /// <summary>
    /// Manage loading of all stored game data
    /// </summary>
    public class DatabaseLoader
    {

        private readonly Dictionary<int, Club> _clubsId;

        private readonly Kernel _kernel;
        public DatabaseLoader(Kernel kernel)
        {
            _kernel = kernel;
            _clubsId = new Dictionary<int, Club>();
        }


        private int GetClubId(Club club)
        {
            int res = -1;
            foreach (KeyValuePair<int, Club> kvp in _clubsId)
            {
                if (kvp.Value == club)
                {
                    res = kvp.Key;
                }
            }
            return res;
        }

        private int NextClubId()
        {
            int res = -1;

            foreach(KeyValuePair<int,Club> kvp in _clubsId)
            {
                if (kvp.Key > res)
                {
                    res = kvp.Key;
                }
            }

            res++;
            return res;
        }

        /*
        public void FIFACSV2Joueurs()
        {
            XDocument d = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XElement root = new XElement("Joueurs");
            d.Add(root);

            string[] lines = File.ReadAllLines("Donnees/Joueurs_FIFA.csv",Encoding.UTF8);
            foreach(string line in lines)
            {
                string[] joueur = line.Split(';');
                string nom = joueur[0];
                int age = int.Parse(joueur[1]);
                DateTime naissance = new DateTime(2019 - age, 1, 1);
                string paysNom = joueur[2];
                Country pays = _kernel.String2Country(paysNom);
                if (pays == null)
                {
                    pays = _kernel.String2Country("France");
                }
                int niveau = int.Parse(joueur[3]) - 2;
                int potentiel = int.Parse(joueur[4]) - 2;
                int idclub = 0;
                bool res = int.TryParse(joueur[5],out idclub);
                string postestr = joueur[6];
                Position p = Position.Midfielder;
                switch (postestr)
                {
                    case "GK": p = Position.Goalkeeper;
                        break;
                    case "CB": case "LB": case "RB": case "LCB": case "RCB": case "RDM": p = Position.Defender; 
                        break;
                    case "CDM": case "CM": case "LM": case "LW": case "LWB":  case "RM": case "RCM": case "LDM":  case "RW": case "RWB": p = Position.Midfielder; 
                        break;
                    case "CAM": case "CF": case "ST": case "LAM": case "RF": case "LCM": case "RAM": case "LF": case "LS": case "RS": p = Position.Striker;
                        break;   
                    default : p = Position.Defender;
                        break;
                }
                if(idclub != 0)
                {
                    Utils.Debug(nom);
                    XElement e = new XElement("Joueur");
                    e.Add(new XAttribute("prenom", ""));
                    e.Add(new XAttribute("nom", nom));
                    e.Add(new XAttribute("niveau", niveau));
                    e.Add(new XAttribute("potentiel", potentiel));
                    e.Add(new XAttribute("poste", p.ToString()));
                    e.Add(new XAttribute("club", idclub));
                    root.Add(e);
                }
            }
            d.Save("Donnees/joueursFIFA.xml");
        }*/

        public void AddIdToClubs()
        {
            int id = 0;

            XDocument doc = XDocument.Load(Utils.dataFolderName + "/clubs.xml");

            foreach(XElement x in doc.Descendants("Clubs"))
            {
                foreach(XElement x2 in x.Descendants("Club"))
                {
                    XAttribute attr_id = new XAttribute("id", id);
                    x2.Add(attr_id);
                    id++;
                }

                foreach (XElement x2 in x.Descendants("Selection"))
                {
                    XAttribute attr_id = new XAttribute("id", id);
                    x2.Add(attr_id);
                    id++;
                }


            }

            doc.Save(Utils.dataFolderName + "/clubs_id.xml");

        }

        private void ReplaceCompetitionId()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/competitions.xml");

            foreach(XElement x in doc.Descendants("Club"))
            {
                string nom = x.Attribute("nom").Value;
                Club club = _kernel.String2Club(nom);
                int id_club = GetClubId(club);
                x.RemoveAttributes();
                x.Add(new XAttribute("id", id_club));
            }

            doc.Save(Utils.dataFolderName + "/competitions_id.xml");
        }

        public void Load()
        {
            /*
            LoadLanguages();
            LoadGeography();
            LoadCities();
            LoadStadiums();
            LoadClubs();
            LoadTournaments();
            LoadPlayers();
            LoadManagers();
            InitTeams();
            InitPlayers();
            LoadMedias();
            LoadGamesComments();

            */
            //FIFACSV2Joueurs();
        }

        public void LoadGamesComments()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/actions.xml");
            foreach (XElement e in doc.Descendants("Actions"))
            {
                foreach (XElement e2 in e.Descendants("Action"))
                {
                    string type = e2.Attribute("type").Value;
                    string content = e2.Value;
                    GameEvent gameEvent;
                    switch(type)
                    {
                        case "tir": 
                            gameEvent = GameEvent.Shot; 
                            break;
                        case "but": 
                            gameEvent = GameEvent.Goal; 
                            break;
                        case "but_pen": 
                            gameEvent = GameEvent.PenaltyGoal; 
                            break;
                        case "carton_jaune": 
                            gameEvent = GameEvent.YellowCard; 
                            break;
                        case "carton_rouge": 
                            gameEvent = GameEvent.RedCard; 
                            break;
                        default : 
                            gameEvent = GameEvent.Goal;
                            break;
                    }
                    _kernel.AddMatchCommentary(gameEvent, content);
                }
            }
        }

        public void LoadMedias()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/medias.xml");
            foreach (XElement e in doc.Descendants("Medias"))
            {
                foreach (XElement e2 in e.Descendants("Media"))
                {
                    string name = e2.Attribute("nom").Value;
                    Country country = _kernel.String2Country(e2.Attribute("pays").Value);
                    Media m = new Media(name, country);
                    _kernel.medias.Add(m);

                    foreach (XElement e3 in e2.Descendants("Journaliste"))
                    {
                        string firstName = e3.Attribute("prenom").Value;
                        string lastName = e3.Attribute("nom").Value;
                        int age = int.Parse(e3.Attribute("age").Value);
                        City city = _kernel.String2City(e3.Attribute("ville").Value);
                        int offset = 0;
                        bool isNational = false;
                        if (e3.Attribute("retrait") != null)
                        {
                            offset = int.Parse(e3.Attribute("retrait").Value);
                        }
                        if (e3.Attribute("national") != null)
                        {
                            if(e3.Attribute("national").Value == "oui")
                            {
                                isNational = true;
                            }
                        }

                        if (city == null)
                        {
                            Utils.Debug(e3.Attribute("ville").Value + " n'est pas une ville.");
                        }
                        Journalist j = new Journalist(firstName, lastName, age, city, offset, isNational);
                        m.journalists.Add(j);
                    }

                    foreach (XElement e3 in e2.Descendants("Couvre"))
                    {
                        int index = int.Parse(e3.Attribute("aPartir").Value);
                        Tournament tournament = _kernel.String2Tournament(e3.Attribute("competition").Value);
                        int averageGames = -1;
                        int multiplexMinGames = -1;
                        int level = -1;
                        if (e3.Attribute("matchParMultiplex") != null)
                        {
                            averageGames = int.Parse(e3.Attribute("matchParMultiplex").Value);
                        }

                        if (e3.Attribute("multiplex") != null)
                        {
                            multiplexMinGames = int.Parse(e3.Attribute("multiplex").Value);
                        }
                        if (e3.Attribute("level") != null)
                        {
                            level = int.Parse(e3.Attribute("level").Value);
                        }

                        m.coverages.Add(new TournamentCoverage(tournament, index, multiplexMinGames, averageGames, level));
                    }
                }
            }
        }

        public void LoadPlayers()
        {
            StreamReader reader = new StreamReader(Utils.dataFolderName + "/joueurs.xml", Encoding.UTF8);
            XDocument doc = XDocument.Load(reader);
            foreach (XElement e in doc.Descendants("Joueurs"))
            {
                foreach (XElement e2 in e.Descendants("Joueur"))
                {
                    string lastName = e2.Attribute("nom").Value;
                    string firstName = e2.Attribute("prenom").Value;
                    int level = int.Parse(e2.Attribute("niveau").Value);
                    int potential = int.Parse(e2.Attribute("potentiel").Value);
                    int clubId = int.Parse(e2.Attribute("club").Value);
                    CityClub club = _clubsId[clubId] as CityClub;
                    Position position;
                    string positionName = e2.Attribute("poste").Value;
                    switch(positionName)
                    {
                        case "DEFENSEUR": 
                            position = Position.Defender; 
                            break;
                        case "MILIEU": 
                            position = Position.Midfielder; 
                            break;
                        case "ATTAQUANT": 
                            position = Position.Striker; 
                            break;
                        default :
                            position = Position.Goalkeeper;
                            break;
                    }
                    Player j = new Player(firstName, lastName, new DateTime(1995, 1, 1), level, potential, _kernel.String2Country("France"), position);
                    club.AddPlayer(new Contract(j, j.EstimateWage(), new DateTime(Session.Instance.Random(Utils.beginningYear,Utils.beginningYear+5), 7, 1), new DateTime(Session.Instance.Game.date.Year, Session.Instance.Game.date.Month, Session.Instance.Game.date.Day)));
                }
            }
        }

        public void LoadManagers()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/entraineurs.xml");
            foreach (XElement e in doc.Descendants("Entraineurs"))
            {
                foreach (XElement e2 in e.Descendants("Entraineur"))
                {
                    string lastName = e2.Attribute("nom").Value;
                    string firstName = e2.Attribute("prenom").Value;
                    int level = int.Parse(e2.Attribute("niveau").Value);
                    string clubName = e2.Attribute("club").Value;
                    CityClub club = _kernel.String2Club(clubName) as CityClub;
                    string countryName = e2.Attribute("nationalite").Value;
                    Country country = _kernel.String2Country(countryName);
                    Manager manager = new Manager(firstName, lastName, level, new DateTime(1970, 1, 1), country);
                    club.manager = manager;
                }
            }
        }

        public void LoadCities()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/villes.xml");
            foreach (XElement e in doc.Descendants("Villes"))
            {
                foreach (XElement e2 in e.Descendants("Ville"))
                {
                    string name = e2.Element("Nom").Value;
                    int population = int.Parse(e2.Element("Population").Value);
                    float lat = float.Parse(e2.Element("Latitute").Value, CultureInfo.InvariantCulture);
                    float lon = float.Parse(e2.Element("Longitude").Value, CultureInfo.InvariantCulture);
                    _kernel.String2Country("France").cities.Add(new City(name, population, lat, lon));

                }
            }
        }

        public void LoadGeography()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/continents.xml");
            foreach (XElement e in doc.Descendants("Monde"))
            {
                foreach (XElement e2 in e.Descendants("Continent"))
                {
                    string continentName = e2.Attribute("nom").Value;
                    Continent c = new Continent(continentName);
                    foreach(XElement e3 in e2.Descendants("Pays"))
                    {
                        string countryName = e3.Attribute("nom").Value;
                        string language = e3.Attribute("langue").Value;
                        Language l = _kernel.String2Language(language);
                        Country p = new Country(countryName,l);
                        foreach(XElement e4 in e3.Descendants("Ville"))
                        {
                            string cityName = e4.Attribute("nom").Value;
                            int population = int.Parse(e4.Attribute("population").Value);
                            float lat = float.Parse(e4.Attribute("Latitute").Value);
                            float lon = float.Parse(e4.Attribute("Longitude").Value);

                            City v = new City(cityName, population, lat, lon);
                            p.cities.Add(v);
                        }
                        c.countries.Add(p);
                    }
                    _kernel.continents.Add(c);
                }
            }
        }

        public void LoadStadiums()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/stades.xml");
            foreach (XElement e in doc.Descendants("Stades"))
            {
                foreach (XElement e2 in e.Descendants("Stade"))
                {
                    string name = e2.Attribute("nom").Value;
                    int capacity = int.Parse(e2.Attribute("capacite").Value);
                    string cityName = e2.Attribute("ville").Value;
                    City v = _kernel.String2City(cityName);
                    Stadium s = new Stadium(name, capacity, v);
                    v.Country().stadiums.Add(s);
                }
            }
        }

        public void LoadClubs()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/clubs.xml");
            
            foreach(XElement e in doc.Descendants("Clubs"))
            {
                foreach (XElement e2 in e.Descendants("Club"))
                {
                    int id = int.Parse(e2.Attribute("id").Value);
                    string name = e2.Attribute("nom").Value;
                    string shortName = e2.Attribute("nomCourt").Value;
                    if (shortName == "")
                    {
                        shortName = name;
                    }
                    int reputation = int.Parse(e2.Attribute("reputation").Value);
                    int budget = int.Parse(e2.Attribute("budget").Value);
                    int supporters = int.Parse(e2.Attribute("supporters").Value);

                    string cityName = e2.Attribute("ville").Value;
                    City city = _kernel.String2City(cityName);

                    Stadium stadium = null;
                    if (e2.Attribute("stade") != null)
                    {
                        string stadiumName = e2.Attribute("stade").Value;
                        stadium = _kernel.String2Stadium(stadiumName);
                        if(stadium == null)
                        {
                            int capacite = 1000;
                            if (city != null)
                            {
                                capacite = city.Population / 10;
                            }
                            stadium = new Stadium(stadiumName, capacite, city);
                            if (city != null)
                            {
                                city.Country().stadiums.Add(stadium);
                            }
                            else
                            {
                                Utils.Debug("La ville " + stadiumName + " n'existe pas.");
                            }
                        }
                    }

                    if (stadium == null)
                    {
                        stadium = new Stadium("Stade de " + shortName, city.Population / 10, city);
                    }


                    int centreFormation = int.Parse(e2.Attribute("centreFormation").Value);
                    string logo = e2.Attribute("logo").Value;
                    if (logo == "" ||
                        !File.Exists(System.IO.Directory.GetCurrentDirectory() + "\\"+Utils.imagesFolderName+"\\"+Utils.clubLogoFolderName+"\\" + logo + ".png"))
                    {
                        logo = "generic";
                    }

                    string musiqueBut = "";
                    if (e2.Attribute("musiqueBut") != null)
                    {
                        musiqueBut = e2.Attribute("musiqueBut").Value ;
                    }
                    else
                    {
                        musiqueBut = "null";
                    }

                    //Simplification
                    reputation = centreFormation;

                    Country pays = city.Country();
                    Manager entraineur = new Manager(pays.language.GetFirstName(), pays.language.GetLastName(), centreFormation, new DateTime(1970, 1, 1), pays);

                    bool equipePremiere = true;
                    Club c = new CityClub(name,entraineur, shortName, reputation, budget, supporters, centreFormation, city, logo, stadium,musiqueBut, equipePremiere);
                    _clubsId[id] = c;
                    _kernel.Clubs.Add(c);
                }
                foreach (XElement e2 in e.Descendants("Selection"))
                {
                    int id = int.Parse(e2.Attribute("id").Value);
                    string name = e2.Attribute("nom").Value;
                    string shortName = name;
                    int reputation = int.Parse(e2.Attribute("reputation").Value);
                    int supporters = int.Parse(e2.Attribute("supporters").Value);
                    Country country = _kernel.String2Country(e2.Attribute("pays").Value);

                    Stadium stadium = null;
                    if (e2.Attribute("stade") != null)
                    {
                        string nom_stade = e2.Attribute("stade").Value;
                        stadium = _kernel.String2Stadium(nom_stade);
                    }

                    if (stadium == null)
                    {
                        stadium = new Stadium("Stade de " + shortName, 15000, null);
                    }
                    int formationFacilities = int.Parse(e2.Attribute("centreFormation").Value);
                    string logo = e2.Attribute("logo").Value;
                    logo = country.Flag;
                    float coefficient = float.Parse(e2.Attribute("coefficient").Value);

                    string goalMusic = "";
                    if (e2.Attribute("musiqueBut") != null)
                    {
                        goalMusic = e2.Attribute("musiqueBut").Value;
                    }
                    else
                    {
                        goalMusic = "null";
                    }

                    Manager entraineur = new Manager(country.language.GetFirstName(), country.language.GetLastName(), formationFacilities, new DateTime(1970, 1, 1), country);

                    Club c = new NationalTeam(name,entraineur, shortName, reputation, supporters, formationFacilities, logo, stadium, coefficient,country,goalMusic);
                    _clubsId[id] = c;
                    _kernel.Clubs.Add(c);
                }
            }
        }
        
        public void LoadTournaments()
        {
            XDocument doc = XDocument.Load(Utils.dataFolderName + "/competitions.xml");

            //Chargement préliminaire de toutes les compétitons pour les référancer
            foreach (XElement e in doc.Descendants("Competitions"))
            {
                foreach (XElement e2 in e.Descendants("Competition"))
                {
                    string name = e2.Attribute("nom").Value;
                    string shortName = e2.Attribute("nomCourt").Value;
                    string logo = e2.Attribute("logo").Value;
                    string seasonBeginning = e2.Attribute("debut_saison").Value;
                    bool isChampionship = e2.Attribute("championnat").Value == "oui" ? true : false;
                    int level = int.Parse(e2.Attribute("niveau").Value);
                    ILocalisation localisation = _kernel.String2Localisation(e2.Attribute("localisation").Value);
                    DateTime debut = String2Date(seasonBeginning);
                    int periodicity = 1;
                    if (e2.Attribute("periodicite") != null)
                    {
                        periodicity = int.Parse(e2.Attribute("periodicite").Value);
                    }
                    int remainingYears = 1;
                    if (e2.Attribute("anneesRestantes") != null)
                    {
                        remainingYears = int.Parse(e2.Attribute("anneesRestantes").Value);
                    }
                    string[] colorStr = e2.Attribute("color").Value.Split(',');
                    Color color = new Color(byte.Parse(colorStr[0]), byte.Parse(colorStr[1]), byte.Parse(colorStr[2]));

                    Tournament tournament = new Tournament(name, logo, debut, shortName, isChampionship, level, periodicity, remainingYears, color);
                    localisation.Tournaments().Add(tournament);
                    //_gestionnaire.Competitions.Add(c);
                }
            }

            //Chargement détaillé de toutes les compétitions
            foreach (XElement e in doc.Descendants("Competitions"))
            {
                foreach(XElement e2 in e.Descendants("Competition"))
                {
                    string name = e2.Attribute("nom").Value;
                    Tournament c = _kernel.String2Tournament(name);
                    foreach(XElement e3 in e2.Descendants("Tour"))
                    {
                        Round round = null;
                        string type = e3.Attribute("type").Value;
                        string nomTour = e3.Attribute("nom").Value;
                        bool twoLegged = e3.Attribute("allerRetour").Value == "oui";
                        string hourByDefault = e3.Attribute("heureParDefaut").Value;
                        DateTime initialisationDate = String2Date(e3.Attribute("initialisation").Value);
                        DateTime endDate = String2Date(e3.Attribute("fin").Value);
                        List<DateTime> dates = new List<DateTime>();
                        bool weCan = true;
                        int i = 1;
                        while (weCan)
                        {
                            if (e3.Attribute("j" + i) != null)
                            {
                                string dayDate = e3.Attribute("j" + i).Value;
                                DateTime dt = String2Date(dayDate);
                                i++;
                                dates.Add(dt);
                            }
                            else
                            {
                                weCan = false;
                            }
                        }

                        if (type == "championnat")
                        {
                            int dernieresJourneesMemeJour = int.Parse(e3.Attribute("dernieresJourneesMemeJour").Value);

                            round = new ChampionshipRound(nomTour, String2Hour(hourByDefault), dates, twoLegged,new List<TvOffset>(), initialisationDate, endDate, dernieresJourneesMemeJour);
                        }
                        else if(type=="elimination")
                        {
                            round = new KnockoutRound(nomTour, String2Hour(hourByDefault), dates, new List<TvOffset>(), twoLegged, initialisationDate, endDate);
                        }
                        else if(type =="poules")
                        {
                            int groupsNumber = int.Parse(e3.Attribute("nombrePoules").Value);
                            RandomDrawingMethod method = String2DrawingMethod(e3.Attribute("methode").Value);
                            round = new GroupsRound(nomTour, String2Hour(hourByDefault), dates, new List<TvOffset>(), groupsNumber, twoLegged, initialisationDate, endDate, method);

                            if(method == RandomDrawingMethod.Geographic)
                            {
                                //Lecture position poules
                                for (int groupNum = 1; groupNum <= groupsNumber; groupNum++)
                                {
                                    string[] poulePosition = e3.Attribute("poule" + groupNum).Value.Split(';');
                                    float latitude = float.Parse(poulePosition[0], CultureInfo.InvariantCulture);
                                    float longitude = float.Parse(poulePosition[1], CultureInfo.InvariantCulture);
                                    GroupsRound tp = round as GroupsRound;
                                    tp.groupsLocalisation.Add(new GeographicPosition(latitude, longitude));
                                }
                            }

                            foreach(XElement eNoms in e3.Descendants("Nom"))
                            {
                                GroupsRound tp = round as GroupsRound;
                                tp.AddGroupName(eNoms.Value);
                            }
                            
                        }
                        else if (type == "inactif")
                        {
                            round = new InactiveRound(nomTour, String2Hour(hourByDefault), initialisationDate, endDate);
                        }
                        c.rounds.Add(round);
                        foreach (XElement e4 in e3.Descendants("Club"))
                        {
                            Club club;
                            int clubId = int.Parse(e4.Attribute("id").Value);
                            if(e4.Attribute("reserve") == null)
                            {
                                club = _clubsId[clubId];
                            }
                            else
                            {
                                CityClub firstTeam = _clubsId[int.Parse(e4.Attribute("id").Value)] as CityClub;
                                string nameAddon = " B";
                                float divider = 1.75f;
                                if (firstTeam.reserves.Count == 1)
                                {
                                    nameAddon = " C"; divider = 2.5f;
                                }
                                if (firstTeam.reserves.Count == 2)
                                {
                                    nameAddon = " D"; divider = 3.5f;
                                }
                                if (firstTeam.reserves.Count == 3)
                                {
                                    nameAddon = " E"; divider = 4.5f;
                                }
                                club = new ReserveClub(firstTeam, firstTeam.name + nameAddon, firstTeam.shortName + nameAddon, null);
                                int newId = NextClubId();
                                _clubsId[newId] = club;
                                _kernel.Clubs.Add(club);
                                firstTeam.reserves.Add(club as ReserveClub);
                                //A reserve team was generated, let's create same players in base club to populate this reserve team
                                int averagePotential = (int)(firstTeam.formationFacilities - (firstTeam.formationFacilities / divider));
                                //Warning for {2,5,5,3} -> 15 is used in team initialisation to determine player number of first team
                                for (int g = 0; g < 2; g++)
                                {
                                    firstTeam.GeneratePlayer(Position.Goalkeeper, 16, 23, -averagePotential);
                                }
                                for (int g = 0; g < 5; g++)
                                {
                                    firstTeam.GeneratePlayer(Position.Defender , 16, 23, -averagePotential);
                                }
                                for (int g = 0; g < 5; g++)
                                {
                                    firstTeam.GeneratePlayer(Position.Midfielder, 16, 23, -averagePotential);
                                }
                                for (int g = 0; g < 3; g++)
                                {
                                    firstTeam.GeneratePlayer(Position.Striker, 16, 23, -averagePotential);
                                }
                            }

                           
                            round.clubs.Add(club);
                        }
                        foreach(XElement e4 in e3.Descendants("Participants"))
                        {
                            int number = int.Parse(e4.Attribute("nombre").Value);
                            IRecoverableTeams source = null;
                            XAttribute continent = e4.Attribute("continent");
                            if(continent != null)
                            {
                                source = _kernel.String2Continent(continent.Value);
                            }
                            else
                            {
                                string competitionName = e4.Attribute("competition").Value;
                                int tourIndex = int.Parse(e4.Attribute("idTour").Value);
                                Tournament comp = _kernel.String2Tournament(competitionName);
                                Round r = comp.rounds[tourIndex];
                                source = r;
                            }
                            RecuperationMethod method;
                            switch(e4.Attribute("methode").Value)
                            {
                                case "meilleurs":
                                    method = RecuperationMethod.Best; 
                                    break;
                                case "pires":
                                    method = RecuperationMethod.Worst; 
                                    break;
                                case "aleatoire":
                                    method = RecuperationMethod.Randomly; 
                                    break;
                                default :
                                    method = RecuperationMethod.Best;
                                    break;
                            }
                            round.recuperedTeams.Add(new RecoverTeams(source, number, method));
                        }
                        foreach (XElement e4 in e3.Descendants("Decalage"))
                        {
                            int day = int.Parse(e4.Attribute("jour").Value);
                            Hour hour = String2Hour(e4.Attribute("heure").Value);
                            int probability = 1;
                            bool isPrimeTime = false;
                            if (e4.Attribute("probabilite") != null)
                            {
                                probability = int.Parse(e4.Attribute("probabilite").Value);
                            }
                            int matchDay = 0;
                            if (e4.Attribute("journee") != null)
                            {
                                matchDay = int.Parse(e4.Attribute("journee").Value);
                            }
                            if (e4.Attribute("prime_time") != null)
                            {
                                if(e4.Attribute("prime_time").Value == "oui")
                                {
                                    isPrimeTime = true;
                                }
                            }
                            TvOffset dtv = new TvOffset(day, hour, probability, matchDay, isPrimeTime);
                            round.programmation.tvScheduling.Add(dtv);
                        }
                        foreach (XElement e4 in e3.Descendants("Regle"))
                        {
                            Rule rule = Rule.AtHomeIfTwoLevelDifference;
                            switch(e4.Attribute("nom").Value)
                            {
                                case "RECOIT_SI_DEUX_DIVISION_ECART": 
                                    rule = Rule.AtHomeIfTwoLevelDifference; 
                                    break;
                                case "EQUIPES_PREMIERES_UNIQUEMENT":
                                    rule = Rule.OnlyFirstTeams;
                                    break;
                                case "RESERVES_NE_MONTENT_PAS": 
                                    rule = Rule.ReservesAreNotPromoted; 
                                    break;
                                default:
                                    rule = Rule.OnlyFirstTeams;
                                    break;
                            }
                            round.rules.Add(rule);
                        }
                        foreach (XElement e4 in e3.Descendants("Dotation"))
                        {
                            int ranking = int.Parse(e4.Attribute("classement").Value);
                            int prize = int.Parse(e4.Attribute("somme").Value);
                            round.prizes.Add(new Prize(ranking, prize));
                        }
                        foreach (XElement e4 in e3.Descendants("Qualification"))
                        {
                            int tourId = int.Parse(e4.Attribute("id_tour").Value);
                            bool nextYear = false;
                            if (e4.Attribute("anneeSuivante") != null)
                            {
                                nextYear = e4.Attribute("anneeSuivante").Value == "oui";
                            }
                            Tournament targetedTournament = null;
                            if (e4.Attribute("competition") != null)
                            {
                                targetedTournament = _kernel.String2Tournament(e4.Attribute("competition").Value);
                            }
                            else
                            {
                                targetedTournament = c;
                            }

                            //Deux cas
                            //1- On a un attribut "classement", avec un classement précis
                            //2- On a deux attributs "de", "a", qui concerne une plage de classement
                            if (e4.Attribute("classement") != null)
                            {
                                int ranking = int.Parse(e4.Attribute("classement").Value);

                                Qualification qu = new Qualification(ranking, tourId, targetedTournament, nextYear);
                                round.qualifications.Add(qu);
                            }
                            else
                            {
                                int from = int.Parse(e4.Attribute("de").Value);
                                int to = int.Parse(e4.Attribute("a").Value);
                                for(int j = from; j<= to; j++)
                                {
                                    Qualification qu = new Qualification(j, tourId, targetedTournament, nextYear);
                                    round.qualifications.Add(qu);
                                }
                            }
                        }
                    }
                }
            }

            foreach(Tournament c in _kernel.Competitions)
            {
                c.InitializeQualificationsNextYearsLists();
            }
        }

        public void LoadLanguages()
        {
            LoadLanguage("Francais", "fr");
            LoadLanguage("Anglais", "en");
        }

        private void LoadLanguage(string languageName, string filename)
        {
            Language language = new Language(languageName);
            string[] text = System.IO.File.ReadAllLines(Utils.dataFolderName + "/" + Utils.namesSubfolderName + "/" + filename + "_p.txt", Encoding.UTF8);
            foreach(string line in text)
            {
                language.AddFirstName(line);
            }
            text = System.IO.File.ReadAllLines(Utils.dataFolderName + "/" + Utils.namesSubfolderName + "/" + filename + "_n.txt", Encoding.UTF8);
            foreach (string line in text)
            {
                language.AddLastName(line);
            }
            _kernel.languages.Add(language);
        }

        public void InitTeams()
        {
            foreach(Club c in _kernel.Clubs)
            {
                CityClub cityClub = c as CityClub;
                if(cityClub != null)
                {
                    int firstTeamPlayersNumber = cityClub.contracts.Count - (cityClub.reserves.Count * 15);
                    int missingContractNumber = 19 - firstTeamPlayersNumber;
                    for (int i = 0; i < missingContractNumber; i++)
                    {
                        cityClub.GeneratePlayer(24,33);
                    }
                }
            }
        }

        public void InitPlayers()
        {
            foreach(Club c in _kernel.Clubs)
            {
                NationalTeam nationalTeam = c as NationalTeam;
                if(nationalTeam != null)
                {
                    int gap = 25 - _kernel.NumberPlayersOfCountry(nationalTeam.country); 
                    if(gap > 0)
                    {
                        for(int i =0; i<gap; i++)
                        {
                            string firstName = nationalTeam.country.language.GetFirstName();
                            string lastName = nationalTeam.country.language.GetLastName();
                            DateTime birthday = new DateTime(Session.Instance.Random(Utils.beginningYear-30, Utils.beginningYear - 18) , Session.Instance.Random(1,13), Session.Instance.Random(1,29));
                            Position p;
                            switch(Session.Instance.Random(1,10))
                            {
                                case 1: case 2: case 3: p = Position.Defender;  break;
                                case 4: case 5: case 6: p = Position.Midfielder;  break;
                                case 7: case 8: p = Position.Striker;  break;
                                default:
                                    p = Position.Goalkeeper;
                                    break;
                            }
                            int level = nationalTeam.formationFacilities + Session.Instance.Random(-5, 5);
                            if(level < 0)
                            {
                                level = 0;
                            }
                            if(level > 99)
                            {
                                level = 99;
                            }
                            Player j = new Player(firstName, lastName, birthday, level, level + 2, nationalTeam.country, p);
                            _kernel.freePlayers.Add(j);
                        }
                    }
                }
            }
            _kernel.NationalTeamsCall();
        }

        private DateTime String2Date(string date)
        {
            string[] split = date.Split('/');
            DateTime d = new DateTime(2000, int.Parse(split[1]), int.Parse(split[0]));
            return d;
        }

        private Hour String2Hour(string hour)
        {
            string[] split = hour.Split(':');
            Hour h = new Hour();
            h.Hours = int.Parse(split[0]);
            h.Minutes = int.Parse(split[1]);
            return h;
        }

        private RandomDrawingMethod String2DrawingMethod(string method)
        {
            RandomDrawingMethod res = RandomDrawingMethod.Level;

            if (method == "Niveau")
            {
                res = RandomDrawingMethod.Level;
            }
            else if (method == "Geographique")
            {
                res = RandomDrawingMethod.Geographic;
            }

            return res;
        }
    }
}