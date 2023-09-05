using Robocode;
using Robocode.Util;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Data;

namespace CAP4053.Student
{
    public class Bubbles : TeamRobot
    {
        int moveDirection = 1;
        double enemyEnergy = 100;
        bool locked = false;
        StateMachine brain = new StateMachine(0.5, 400);
        String target = "";
        int leaderRank = 1;
        Random rnd= new Random();
        bool passed = false;
        bool cantFindTarget = false;
        public override void Run()
        {
            SetColors(ColorTranslator.FromHtml("#B482C2"), ColorTranslator.FromHtml("#B482C2"), ColorTranslator.FromHtml("#B482C2"), ColorTranslator.FromHtml("#B482C2"), ColorTranslator.FromHtml("#B482C2"));
            IsAdjustRadarForGunTurn = true;
            IsAdjustRadarForRobotTurn = true;
            IsAdjustGunForRobotTurn = true;

            //Radar Scanning
            TurnRadarRightRadians(double.MaxValue);
            
            do
            {
                if (GetScannedRobotEvents().Count == 0 || !locked)
                    TurnRadarRightRadians(double.MaxValue);
                
                Scan();
                Execute();
            } while (true);

        }
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            //if no teammates you are the leader
            if (Teammates == null)
                leaderRank = 2;
            Console.WriteLine(target);
            Console.WriteLine("IsTeammate: " + IsTeammate(e.Name) + " Leader: " + leaderRank);
            
            //Only lock onto enemies and when in a team lock on to the target specified by the leader
            if (IsTeammate(e.Name) || ((!target.Equals(e.Name) && leaderRank < 2) || target.Equals("")) && !cantFindTarget)
            {
                Console.WriteLine("Made it");
                target = "";
                locked = false;
                cantFindTarget = true;
                return;
            }
            else
            {
                locked = true;
                target = e.Name;
                cantFindTarget = false;
            }

            //Check if leader and send target if you are
            if(leaderRank >= 2 && Teammates != null && !passed)
            {
                try
                {
                    BroadcastMessage(e.Name);
                }
                catch (Exception)
                {
                }

            }

            if (Energy < 20 && Teammates != null)
            {
                BroadcastMessage(2);
                passed = true;
            }

            double enemyAbsBearing = HeadingRadians + e.BearingRadians;

            //Radar Lock
            double radarTurn = enemyAbsBearing - RadarHeadingRadians;
            SetTurnRadarRightRadians(2 * Utils.NormalRelativeAngle(radarTurn));

            //Move
            // switch directions if the enemy has fired OR randomly if enemy has <20 energy
            if (brain.GetDodge()) { 
                if ((enemyEnergy - e.Energy >= 0.1 && enemyEnergy - e.Energy <= 3) || (e.Energy<20 && rnd.Next(0, 20) == 0))
                    moveDirection *= -1;    
            }
            enemyEnergy = e.Energy;
            // circle enemy and maintain 400px distance
            /*if (e.Distance < 400)
                SetTurnRight(e.Bearing + 90 + 30 * moveDirection);
            else
                SetTurnRight(e.Bearing + 90);
            */

            SetTurnRight(e.Bearing + 90 + brain.GetAngle() * moveDirection);
            SetAhead(1000 * moveDirection);

            //Aim & Fire
            double gunTurn = enemyAbsBearing - GunHeadingRadians;
            SetTurnGunRightRadians(Utils.NormalRelativeAngle(gunTurn));
            SetFire(brain.GetPower() / e.Distance);

            brain.Update(Energy / e.Energy, e.Distance);

        }

        public override void OnHitWall(HitWallEvent evnt)
        {
            moveDirection *= -1;
        }

        public override void OnMessageReceived(MessageEvent e)
        {  
            Console.WriteLine("***Message Recieved: " + e.Message);
            if (e.Message is String)
            {
                target = (String)e.Message;
            }
            if(e.Message is int)
            {
                
                if (((int)e.Message + 1).Equals(2))
                    leaderRank = 2;
            }
        }

        private class StateMachine
        {
            private class State
            {
                public String name;
                public int circleAngle;
                public int firePower;
                public bool dodge;

                public State(String name, int circleAngle, int firePower, bool dodge)
                {
                    this.name = name;
                    this.circleAngle = circleAngle;
                    this.firePower = firePower;
                    this.dodge = dodge;
                }
            }

            Dictionary<String, State> states = new Dictionary<String, State>();
            State current;
            double Distance;
            double Energypercent;

            public StateMachine(double EnergyRatio, double Distance) {
                states.Add("standard", new State("standard", 0, 400,true));
                states.Add("distance", new State("distance", 30, 400, true));
                states.Add("ram", new State("ram", -90, 400,false));
                states.Add("low power", new State("low power", -90, 200,true));
                states.Add("distance lp", new State("distance lp", 30, 200,true));
                current = states["standard"];

                this.Distance = Distance;
                this.Energypercent = EnergyRatio;
            }

            public void Update(double energyRatio, double distance)
            {
                bool close = false;
                int energy = 0;

                if (distance < Distance)
                    close = true;

                if (energyRatio > 1.0 + Energypercent)
                    energy = 1;

                if (energyRatio < 1.0 - Energypercent)
                    energy = -1;

                Transition(energy, close);
            }

            void Transition(int energy, bool close)
            {
                if (energy == -1 && close)
                {
                    current = states["distance lp"];
                    return;
                }

                if (energy == -1 && !close)
                {
                    current = states["low power"];
                    return;
                }

                if (energy == 0 && close)
                {
                    current = states["distance"];
                    return;
                }

                if (energy == 0 && !close)
                {
                    current = states["standard"];
                    return;
                }

                if (energy == 1)
                {
                    current = states["ram"];
                    return;
                }
            }

            public int GetAngle()
            {
                Console.WriteLine("State: " + current.name);
                return current.circleAngle;
            }

            public double GetPower()
            {
                return current.firePower;
            }

            public bool GetDodge()
            {
                return current.dodge;
            }
        }
    }
}

