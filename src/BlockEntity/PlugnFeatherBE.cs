using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class PlugnFeatherBE : BlockEntity
    {
        /// <summary>Position of master block.</summary>
        public Vec3i master;

        /// <summary>Positions of slave blocks.</summary>
        public List<Vec3i> slaves = new List<Vec3i>();
        public int slaveCount = 0;

        /// <summary>Orientation that this block is facing. Up, Down, Horizontal.</summary>
        public string orientation;

        /// <summary>Direction that this block is facing. North, South, East, West.</summary>
        public string facing;

        public int state = 0;
        public int maxstate = 2;
        public int work = 0;
        public int maxwork = 100;


        public bool SetWork(int n)
        {
            //sets work between 0 and max work.
            if (n >= 0 && n <= maxwork)
            {
                work = n;
                return true;
            }
            return false;
        }
        public bool IncreaseWork(int n)
        {
            //Increases the amount of work, if the amount is more than the max allowed will return true to allow for the calling method to change the state.
            if (n == 0)
            {
                return false;
            }

            if (work + n > maxwork)
            {
                work += (n - maxwork);
                return true;
            }

            work += n;
            return false;
        }
        public bool SetMaxWork(int n)
        {
            if (n >= 0)
            {
                maxwork = n;
                return true;
            }
            return false;
        }

        public bool IncreaseState(int n)
        {
            if (n == 0)
            {
                return false;
            }
            if (state + n > maxstate)
            {
                state = maxstate;
                return true;
            }
            state += 1;

            return true;
        }
        public bool SetState(int n)
        {
            // returns false if n is out of range.
            if (n <= maxstate && n >= 0)
            {
                state = n;
                return true;
            }
            return false;
        }


        public bool AddSlave(Vec3i slave)
        {
            if (slave == null)
            {
                return false;
            }
            slaves.Add(slave);
            slaveCount = slaves.Count;
            return true;
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("slavecount", slaveCount);
            tree.SetString("orientation", orientation);
            tree.SetString("facing", facing);

            tree.SetInt("state", state);
            tree.SetInt("work", work);
            tree.SetInt("maxwork", maxwork);

            if (slaves.Count != 0)
            {
                for (int i = 0; i < slaves.Count; i++)
                {
                    string savestringx = "slave" + i + "x";
                    string savestringy = "slave" + i + "y";
                    string savestringz = "slave" + i + "z";

                    tree.SetInt(savestringx, slaves[i].X);
                    tree.SetInt(savestringy, slaves[i].Y);
                    tree.SetInt(savestringz, slaves[i].Z);
                }
            }
            if (master != null)
            {
                tree.SetInt("masterx", master.X);
                tree.SetInt("mastery", master.Y);
                tree.SetInt("masterz", master.Z);
            }

            base.ToTreeAttributes(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            slaveCount = tree.GetInt("slavecount");
            orientation = tree.GetString("orientation");
            facing = tree.GetString("facing");

            state = tree.GetInt("state", state);
            work = tree.GetInt("work", work);
            maxwork = tree.GetInt("maxwork", maxwork);

            if (slaveCount != 0)
            {
                for (int i = 0; i < slaveCount; i++)
                {
                    slaves.Add(new Vec3i(tree.GetInt("slave" + i + "x"), tree.GetInt("slave" + i + "y"), tree.GetInt("slave" + i + "z")));
                }
            }
            if (tree.HasAttribute("masterx"))
            {
                master = new Vec3i(tree.GetInt("masterx"), tree.GetInt("mastery"), tree.GetInt("masterz"));
            }



            base.FromTreeAttributes(tree, worldAccessForResolve);
        }
    }
}
