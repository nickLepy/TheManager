﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TheManager.Exportation;

namespace TheManager
{

    [DataContract]
    public struct TournamentStatistics : IEquatable<TournamentStatistics>
    {
        [DataMember]
        public Match BiggerScore { get; set; }
        [DataMember]
        public Match LargerScore { get; set; }
        [DataMember]
        public KeyValuePair<int, Player> TopGoalscorerOnOneSeason { get; set; }
        [DataMember]
        public KeyValuePair<int, Club> BiggestAttack { get; set; }
        [DataMember]
        public KeyValuePair<int, Club> WeakestAttack { get; set; }
        [DataMember]
        public KeyValuePair<int, Club> BiggestDefense { get; set; }
        [DataMember]
        public KeyValuePair<int, Club> WeakestDefense { get; set; }

        public TournamentStatistics(int i)
        {
            BiggerScore = null;
            LargerScore = null;
            TopGoalscorerOnOneSeason = new KeyValuePair<int, Player>(0, null);
            BiggestAttack = new KeyValuePair<int, Club>(0, null);
            WeakestAttack = new KeyValuePair<int, Club>(0, null);
            BiggestDefense = new KeyValuePair<int, Club>(0, null);
            WeakestDefense = new KeyValuePair<int, Club>(0, null);
        }
        
        public bool Equals(TournamentStatistics other)
        {
            throw new NotImplementedException();
        }
    }

    [DataContract(IsReference =true)]
    public class Tournament
    {

        [DataMember]
        private string _name;
        [DataMember]
        private List<Tour> _rounds;
        [DataMember]
        private string _logo;
        [DataMember]
        private DateTime _seasonBeginning;
        [DataMember]
        private string _shortName;
        [DataMember]
        private List<Club>[] _nextYearQualified;
        [DataMember]
        private bool _isChampionship;
        [DataMember]
        private int _level;
        [DataMember]
        private TournamentStatistics _statistics;
        [DataMember]
        private List<Tournament> _previousEditions;
        [DataMember]
        private int _periodicity;
        [DataMember]
        private int _remainingYears;

        public string name { get => _name; }
        public List<Tour> rounds { get => _rounds; }
        public string logo { get => _logo; }
        [DataMember]
        public int currentRound { get; set; }

        public DateTime seasonBeginning => _seasonBeginning;
        public string shortName => _shortName;
        public List<Tournament> previousEditions => _previousEditions;
        public TournamentStatistics statistics { get => _statistics; set => _statistics = value; }

        /// <summary>
        /// Is a championship (L1, L2)
        /// Fixed : if it's a championship, the main round is the round at index 0
        /// </summary>
        public bool isChampionship => _isChampionship;

        /// <summary>
        /// Level in the hierarchy (L1 = 1, L2 = 2 ...)
        /// </summary>
        public int level => _level;

        public Tournament(string name, string logo, DateTime seasonBeginning, string shortName, bool isChampionship, int level, int periodicity, int remainingYears)
        {
            _rounds = new List<Tour>();
            _name = name;
            _logo = logo;
            _seasonBeginning = seasonBeginning;
            currentRound = -1;
            _shortName = shortName;
            _isChampionship = isChampionship;
            _level = level;
            _statistics = new TournamentStatistics(0);
            _previousEditions = new List<Tournament>();
            _periodicity = periodicity;
            _remainingYears = remainingYears;
        }

        public void InitializeQualificationsNextYearsLists()
        {
            _nextYearQualified = new List<Club>[rounds.Count];
            for(int i =0;i < rounds.Count; i++)
            {
                _nextYearQualified[i] = new List<Club>();
            }
        }

        /// <summary>
        /// End of the season, all rounds are reset and qualified teams for next years are dispatched
        /// </summary>
        public void Reset()
        {
            _remainingYears--;
            if (_remainingYears == 0)
            {
                _remainingYears = _periodicity;
            
                UpdateRecords();
                Tournament copyForArchives = new Tournament(_name, _logo, _seasonBeginning, _shortName, _isChampionship, _level, _periodicity, _remainingYears);
                foreach (Tour r in rounds) copyForArchives.rounds.Add(r.Copie());
                copyForArchives.statistics = statistics;
                _previousEditions.Add(copyForArchives);
                for (int i = 0; i<rounds.Count; i++)
                {
                    rounds[i].RAZ();
                    List<Club> clubs = new List<Club>(_nextYearQualified[i]);
                    foreach (Club c in clubs) rounds[i].Clubs.Add(c);
                }
                InitializeQualificationsNextYearsLists();
                currentRound = -1;
                
            }
        }

        private void UpdateRecords()
        {
            foreach(Tour r in _rounds)
            {
                foreach(Match m in r.Matchs)
                {
                    if (_statistics.LargerScore == null || Math.Abs(m.Score1 - m.Score2) > Math.Abs(_statistics.LargerScore.Score1 - _statistics.LargerScore.Score2))
                        _statistics.LargerScore = m;
                    if (_statistics.BiggerScore == null || m.Score1 + m.Score2 > _statistics.BiggerScore.Score1 + _statistics.BiggerScore.Score2)
                        _statistics.BiggerScore = m;
                }
            }
        }

        public void NextRound()
        {
            if(currentRound > -1 && currentRound < _rounds.Count)
                _rounds[currentRound].DistribuerDotations();
            if (_rounds.Count > currentRound + 1)
            {
                currentRound++;
                _rounds[currentRound].Initialiser();
            }

            //Tour 0, championnat -> génère match amicaux
            if(currentRound == 0)
            {
                if (isChampionship)
                {
                    foreach (Club c in rounds[0].Clubs)
                    {
                        CityClub cv = c as CityClub;
                        if (cv != null)
                        {
                            cv.GenerateFriendlyGamesCalendar();
                        }
                    }
                }
            }
            

        }

        /// <summary>
        /// Qualify a club for a round on the next year edition fo the tournament
        /// </summary>
        /// <param name="c">The club to add</param>
        /// <param name="tourIndex">Index of the round where club is qualified</param>
        public void AddClubForNextYear(Club c, int tourIndex)
        {
            _nextYearQualified[tourIndex].Add(c);
        }

        public override string ToString()
        {
            return _name;
        }

        public int AverageAttendance(Club c)
        {
            int i = 0;
            int attendance = 0;
            foreach(Tour t in _rounds)
            {
                foreach(Match m in t.Matchs)
                {
                    if((m.Domicile == c) && m.Joue)
                    {
                        attendance += m.Affluence;
                        i++;
                    }
                }
            }
            return i != 0 ? attendance/i : 0;
        }

        public Club Winner()
        {
            if(isChampionship)
            {
                return _rounds[0].Vainqueur();
            }
            else
            {
                return _rounds[_rounds.Count - 1].Vainqueur();
            }
        }

        public List<KeyValuePair<Player, int>> Goalscorers()
        {
            Dictionary<Player, int> goalscorers = new Dictionary<Player, int>();

            foreach(Tour t in _rounds)
            {
                foreach(KeyValuePair<Player,int> kvp in t.Buteurs())
                {
                    if (goalscorers.ContainsKey(kvp.Key)) goalscorers[kvp.Key] += kvp.Value;
                    else goalscorers[kvp.Key] = kvp.Value;
                }
            }

            List<KeyValuePair<Player, int>> list = goalscorers.ToList();

            list.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            return list;
        }


        /// <summary>
        /// Make the tournament inactive (when used decide to disable tournament)
        /// </summary>
        public void RendreInactive()
        {

            List<Tour> newRounds = new List<Tour>();
            foreach(Tour t in _rounds)
            {
                TourInactif tour = new TourInactif(t.Nom, t.Programmation.HeureParDefaut, t.Programmation.Initialisation, t.Programmation.Fin);
                newRounds.Add(tour);
                foreach (Qualification q in t.Qualifications)
                {

                    //if(t as TourPoules != null)

                    //if(t as TourElimination != null)
                    //else
                    //tour.Qualifications.Add(q);
                }
                foreach (RecuperationEquipes re in t.RecuperationEquipes)
                {
                    tour.RecuperationEquipes.Add(re);
                }
                foreach (Club c in t.Clubs) tour.Clubs.Add(c);
                foreach (Dotation d in t.Dotations) tour.Dotations.Add(d);
                foreach (Rule r in t.Regles) tour.Regles.Add(r);
            }

            foreach(Tournament c in Session.Instance.Partie.kernel.Competitions)
            {
                if(c != this)
                {
                    foreach (Tour t in c.rounds)
                    {
                        for(int i = 0; i<t.RecuperationEquipes.Count; i++)
                        {
                            RecuperationEquipes re = t.RecuperationEquipes[i];
                            if (_rounds.Contains(re.Source))
                            {
                                int index = _rounds.IndexOf(re.Source as Tour);
                                re.Source = newRounds[index];
                            }
                        }
                        
                    }

                }
            }

            _rounds.Clear();
            foreach (Tour t in newRounds) _rounds.Add(t);

            /*
            Tour premierTour = _tours[0];
            Tour dernierTour = _tours[_tours.Count - 1];
            TourInactif ti = new TourInactif("Tour", new Heure() { Heures = 18, Minutes = 0 }, premierTour.Programmation.Initialisation.AddDays(3), dernierTour.Programmation.Fin.AddDays(-3));

            List<IEquipesRecuperables> tours = new List<IEquipesRecuperables>(_tours);

            //qualifications

            //Pour toutes les autres compétitions, quand il faut récupérer des équipes de cette compétition on les récupère depuis le tour que l'on est en train de créer
            //Pour toutes les qualitifcations des autres compétitions vers ce tour, elle se transforment au tour 0 si c'est le tour 0 qui est visé et année suivante, sinon on abandonnne
            foreach (Competition c in Session.Instance.Partie.Gestionnaire.Competitions)
            {
                if(c != this)
                {
                    foreach(Tour t in _tours)
                    {
                        for(int i = 0; i<t.RecuperationEquipes.Count; i++)
                        {
                            RecuperationEquipes re = t.RecuperationEquipes[i];
                            if (tours.Contains(re.Source)) re.Source = ti;
                        }
                        for(int i = 0; i<t.Qualifications.Count; i++)
                        {
                            Qualification q = t.Qualifications[i];
                            if(!(q.Competition == this && q.IDTour == 0 && q.AnneeSuivante == true))
                            {
                                t.Qualifications.Remove(q);
                                i--;
                            }
                        }

                    }
                }
            }


            _tours.Clear();
            _tours.Add(ti);*/

        }

    }
}