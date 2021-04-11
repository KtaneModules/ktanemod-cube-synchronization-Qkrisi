using System.Collections.Generic;

namespace CubeSynchronization
{
    public enum MovementType
    {
        None = 0,
        Up = 1,
        Down = -1,
        Left = 2,
        Right = -2,
        Clockwise = 3,
        CounterClockwise = -3
    }
    
    public struct CubeFace
    {
        public bool Present;
        public int Number;
        public MovementType BackMovement;
        public MovementType SecondaryBackMovement;

        public int ModuleID;
        public int TwitchID;

        public int FrontNumber
        {
            get
            {
                return Number * TwitchID;
            }
        }

        public int BackNumber
        {
            get
            {
                return Number * ModuleID;
            }
        }

        public MovementType FrontMovement
        {
            get
            {
                return (MovementType)((int)BackMovement * -1);
            }
        }

        public MovementType SecondaryFrontMovement
        {
            get
            {
                return (MovementType)((int)SecondaryBackMovement * -1);
            }
        }
    }

    public class Cube
    {
        public CubeFace[] Faces = new CubeFace[6];

        public int[] Empties
        {
            get
            {
                List<int> empties = new List<int>();
                for (int i = 0; i < 6; i++)
                {
                    if (!Faces[i].Present) empties.Add(i);
                }
                return empties.ToArray();
            }
        }
    }
}