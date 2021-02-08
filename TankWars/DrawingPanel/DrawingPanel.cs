//Authors: Gavin Gray, Dan Ruley
//November, 2019
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TankWars;

namespace TankWarsView
{
    /// <summary>
    /// This class defines the drawing panel for the game and contains methods for drawing the various game objects.
    /// </summary>
    public class DrawingPanel : Panel
    {
        private GameWorld TheWorld = null;
        private readonly int HPBAR_WIDTH = 40;
        private readonly int HPBAR_HEIGHT = 5;

        private static readonly string BASE_PATH = "..\\..\\..\\Resources\\Resources\\Images\\";
        private readonly Font NAME_FONT = new Font("Consolas", 10);

        // All the sprites needed in order to draw the game.
        private Image Wall = new Bitmap(BASE_PATH + "WallSprite.png");
        private Image Background = new Bitmap(BASE_PATH + "Background.png");
        private Image Explosion = new Bitmap(BASE_PATH + "Explosion.gif");

        private Image RayPowerup = new Bitmap(BASE_PATH + "RayPowerup.png");

        private Image BlueTank = new Bitmap(BASE_PATH + "BlueTank.png");
        private Image DarkTank = new Bitmap(BASE_PATH + "DarkTank.png");
        private Image GreenTank = new Bitmap(BASE_PATH + "GreenTank.png");
        private Image LightGreenTank = new Bitmap(BASE_PATH + "LightGreenTank.png");
        private Image OrangeTank = new Bitmap(BASE_PATH + "OrangeTank.png");
        private Image PurpleTank = new Bitmap(BASE_PATH + "PurpleTank.png");
        private Image RedTank = new Bitmap(BASE_PATH + "RedTank.png");
        private Image YellowTank = new Bitmap(BASE_PATH + "YellowTank.png");

        private Image BlueProj = new Bitmap(BASE_PATH + "shot_blue.png");
        private Image DarkProj = new Bitmap(BASE_PATH + "shot_grey.png");
        private Image LightProj = new Bitmap(BASE_PATH + "shot-white.png");
        private Image RedProj = new Bitmap(BASE_PATH + "shot_red_new.png");
        private Image PurpleProj = new Bitmap(BASE_PATH + "shot_violet.png");
        private Image YellowProj = new Bitmap(BASE_PATH + "shot-yellow.png");

        private Image BlueTurret = new Bitmap(BASE_PATH + "BlueTurret.png");
        private Image DarkTurret = new Bitmap(BASE_PATH + "DarkTurret.png");
        private Image GreenTurret = new Bitmap(BASE_PATH + "GreenTurret.png");
        private Image LightGreenTurret = new Bitmap(BASE_PATH + "LightGreenTurret.png");
        private Image OrangeTurret = new Bitmap(BASE_PATH + "OrangeTurret.png");
        private Image PurpleTurret = new Bitmap(BASE_PATH + "PurpleTurret.png");
        private Image RedTurret = new Bitmap(BASE_PATH + "RedTurret.png");
        private Image YellowTurret = new Bitmap(BASE_PATH + "YellowTurret.png");

        /// <summary>
        /// Constructs a DrawingPanel object that is responsible for drawing the game world.
        /// </summary>
        public DrawingPanel()
        {
            DoubleBuffered = true;
        }

        /// <summary>
        /// Initializes the Drawing Panel's reference to the game world object.
        /// </summary>
        /// <param name="w">The GameWorld</param>
        public void SetWorldReference(GameWorld w)
        {
            if (TheWorld == null) TheWorld = w;
        }


        public void StartNewExplosion(int id, double x, double y)
        {
            Image ii = (Image)Explosion.Clone();
            Explosion e = new Explosion(ii, new Vector2D(x, y), id);
            TheWorld.AddOrUpdateGameObj(id, e);
        }

        /// <summary>
        /// Helper method for DrawObjectWithTransform
        /// </summary>
        /// <param name="size">The world (and image) size</param>
        /// <param name="w">The worldspace coordinate</param>
        /// <returns></returns>
        private static int WorldSpaceToImageSpace(int size, double w)
        {
            return (int)w + size / 2;
        }

        // A delegate for DrawObjectWithTransform
        // Methods matching this delegate can draw whatever they want using e  
        public delegate void ObjectDrawer(object o, PaintEventArgs e);

        /// <summary>
        /// This method performs a translation and rotation to drawn an object in the world.
        /// </summary>
        /// <param name="e">PaintEventArgs to access the graphics (for drawing)</param>
        /// <param name="o">The object to draw</param>
        /// <param name="worldSize">The size of one edge of the world (assuming the world is square)</param>
        /// <param name="worldX">The X coordinate of the object in world space</param>
        /// <param name="worldY">The Y coordinate of the object in world space</param>
        /// <param name="angle">The orientation of the objec, measured in degrees clockwise from "up"</param>
        /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
        private void DrawObjectWithTransform(PaintEventArgs e, object o, int worldSize, double worldX, double worldY, double angle, ObjectDrawer drawer)
        {
            // "push" the current transform
            System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

            int x = WorldSpaceToImageSpace(worldSize, worldX);
            int y = WorldSpaceToImageSpace(worldSize, worldY);
            e.Graphics.TranslateTransform(x, y);
            if (!System.Double.IsNaN(angle))
            {
                e.Graphics.RotateTransform((float)angle);
            }

            drawer(o, e);

            // "pop" the transform
            e.Graphics.Transform = oldMatrix;
        }

        /// <summary>
        /// Draws a colored rectangle above a tank, representing the tank's current hit points.  
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void HPDrawer(object o, PaintEventArgs e)
        {
            int width = HPBAR_WIDTH;
            int height = HPBAR_HEIGHT;
            Rectangle r;
            using (System.Drawing.SolidBrush redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
            using (System.Drawing.SolidBrush yellowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow))
            using (System.Drawing.SolidBrush greenBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green))
            {
                double ratio = ((Tank)o).hitPoints / (double)TheWorld.STARTINGHP;
                r = new Rectangle(-width / 2, -height / 2, (int)(width * ratio), height);
                if (ratio > 0.7) e.Graphics.FillRectangle(greenBrush, r);
                else if (ratio >= 0.4) e.Graphics.FillRectangle(yellowBrush, r);
                else e.Graphics.FillRectangle(redBrush, r);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name = "o" > The object to draw</param>
        /// <param name = "e" > The PaintEventArgs to access the graphics</param>
        private void PlayerDrawer(object o, PaintEventArgs e)
        {
            int width = TheWorld.TANKWIDTH;
            int height = TheWorld.TANKWIDTH;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);

            switch (((Tank)o).ID % 8)
            {
                case 0:
                    e.Graphics.DrawImage(BlueTank, r);
                    break;
                case 1:
                    e.Graphics.DrawImage(DarkTank, r);
                    break;
                case 2:
                    e.Graphics.DrawImage(GreenTank, r);
                    break;
                case 3:
                    e.Graphics.DrawImage(LightGreenTank, r);
                    break;
                case 4:
                    e.Graphics.DrawImage(OrangeTank, r);
                    break;
                case 5:
                    e.Graphics.DrawImage(PurpleTank, r);
                    break;
                case 6:
                    e.Graphics.DrawImage(RedTank, r);
                    break;
                case 7:
                    e.Graphics.DrawImage(YellowTank, r);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Helper method that draws the player names and scores next to each tank.
        /// </summary>
        private void TextDrawer(object o, PaintEventArgs e)
        {
            string playerscore = ((Tank)o).name + ": " + ((Tank)o).score;
            SizeF strsize = e.Graphics.MeasureString(playerscore, NAME_FONT);
            RectangleF namebox = new RectangleF(new PointF(-(strsize.Width / 2), -(strsize.Height) / 2), strsize);

            using (System.Drawing.SolidBrush whiteBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
            {
                e.Graphics.DrawString(playerscore, NAME_FONT, whiteBrush, namebox);
            }
        }

        /// <summary>
        /// Helper method that draws the tank turrets.
        /// </summary>
        private void TurretDrawer(object o, PaintEventArgs e)
        {
            int width = TheWorld.TURRSIZE;
            int height = TheWorld.TURRSIZE;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
            switch (((Tank)o).ID % 8)
            {
                case 0:
                    e.Graphics.DrawImage(BlueTurret, r);
                    break;
                case 1:
                    e.Graphics.DrawImage(DarkTurret, r);
                    break;
                case 2:
                    e.Graphics.DrawImage(GreenTurret, r);
                    break;
                case 3:
                    e.Graphics.DrawImage(LightGreenTurret, r);
                    break;
                case 4:
                    e.Graphics.DrawImage(OrangeTurret, r);
                    break;
                case 5:
                    e.Graphics.DrawImage(PurpleTurret, r);
                    break;
                case 6:
                    e.Graphics.DrawImage(RedTurret, r);
                    break;
                case 7:
                    e.Graphics.DrawImage(YellowTurret, r);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Draws the background image from the image file.
        /// </summary>
        /// <param name="e"></param>
        private void BackgroundDrawer(PaintEventArgs e)
        {
            int width = TheWorld.SIZE;
            int height = TheWorld.SIZE;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, width, height);
            e.Graphics.DrawImage(Background, r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name = "o" > The object to draw</param>
        /// <param name = "e" > The PaintEventArgs to access the graphics</param>
        private void ProjectileDrawer(object o, PaintEventArgs e)
        {
            int width = TheWorld.PROJSIZE;
            int height = TheWorld.PROJSIZE;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
            switch (((Projectile)o).owner % 8)
            {
                case 0:
                    e.Graphics.DrawImage(BlueProj, r);
                    break;
                case 1:
                    e.Graphics.DrawImage(DarkProj, r);
                    break;
                case 2:
                    e.Graphics.DrawImage(YellowProj, r);
                    break;
                case 3:
                    e.Graphics.DrawImage(LightProj, r);
                    break;
                case 4:
                    e.Graphics.DrawImage(LightProj, r);
                    break;
                case 5:
                    e.Graphics.DrawImage(PurpleProj, r);
                    break;
                case 6:
                    e.Graphics.DrawImage(RedProj, r);
                    break;
                case 7:
                    e.Graphics.DrawImage(YellowProj, r);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name = "o" > The object to draw</param>
        /// <param name = "e" > The PaintEventArgs to access the graphics</param>
        private void WallDrawer(object o, PaintEventArgs e)
        {
            int width = TheWorld.WALLWIDTH;
            int height = TheWorld.WALLWIDTH;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
            e.Graphics.DrawImage(Wall, r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name = "o" > The object to draw</param>
        /// <param name = "e" > The PaintEventArgs to access the graphics</param>
        private void PowerupDrawer(object o, PaintEventArgs e)
        {
            Powerup p = o as Powerup;

            int width = TheWorld.POWSIZE;
            int height = TheWorld.POWSIZE;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
            e.Graphics.DrawImage(RayPowerup, r);
        }

        private void ExplosionDrawer(object o, PaintEventArgs e)
        {
            int width = TheWorld.TANKWIDTH;
            int height = TheWorld.TANKWIDTH;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
            e.Graphics.DrawImage((Image)o, r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform - draws the special attack beam.
        /// </summary>
        private void BeamDrawer(object o, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int width;
            int length = TheWorld.SIZE * 2;
            //set width based on the beam's frame count so it gets narrower 
            width = ((Beam)o).frame_count / 5;
            //set beam color to match its tank owner
            Color c = GetBeamColor(((Beam)o).owner);
            Random rng = new Random();

            Rectangle r = new Rectangle(-width / 2, -length, width, length);

            using (System.Drawing.SolidBrush beam_brush = new System.Drawing.SolidBrush(c))
            {
                e.Graphics.FillRectangle(beam_brush, r);

                //negative one and delt are used to make particles draw on both sides of the beam
                int neg = -1;
                int delt = 1;
                //random offset for particle location along the beam length
                float f = (float)(30 * 1 / rng.Next(3, 9));
                for (int i = 1; i <= 10; i++)
                {
                    //switch delt from negative to positive every other loop
                    delt *= neg;
                    //each frame a beam is drawn, 10 small particles are drawn at random locations along the beam
                    e.Graphics.FillEllipse(beam_brush, new RectangleF((width * f * delt) / 2, -(float)(length * rng.NextDouble()), width, width));
                }

            }

        }

        /// <summary>
        /// Returns the color for a beam attack, depending on the given ID number.  Used to get the beam color that matches a given tank.
        /// </summary>
        private Color GetBeamColor(int ID)
        {
            Color c = new Color();
            switch (ID % 8)
            {
                case 0:
                    c = Color.Blue;
                    break;
                case 1:
                    c = Color.DarkViolet;
                    break;
                case 2:
                    c = Color.Yellow;
                    break;
                case 3:
                    c = Color.White;
                    break;
                case 4:
                    c = Color.White;
                    break;
                case 5:
                    c = Color.Purple;
                    break;
                case 6:
                    c = Color.Red;
                    break;
                case 7:
                    c = Color.Yellow;
                    break;
                default:
                    break;
            }
            return c;
        }

        /// <summary>
        /// Called when the DrawingPanel needs to re-draw.  This is the main driver function for drawing the world each frame after the controller updates the model.
        /// </summary>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Return if the TheWorld hasn't been updated yet.
            if (TheWorld == null) return;

            // If the player is in the world, we need to center the view on him.
            if (TheWorld.GetTanks().ContainsKey(TheWorld.USER_ID))
            {
                Tank player = TheWorld.GetTanks()[TheWorld.USER_ID];
                double playerX = player.location.GetX();
                double playerY = player.location.GetY();

                // calculate view/world size ratio
                double ratio = (double)this.Size.Width / (double)TheWorld.SIZE;
                int halfSizeScaled = (int)(TheWorld.SIZE / 2.0 * ratio);

                double inverseTranslateX = -WorldSpaceToImageSpace(TheWorld.SIZE, playerX) + halfSizeScaled;
                double inverseTranslateY = -WorldSpaceToImageSpace(TheWorld.SIZE, playerY) + halfSizeScaled;

                e.Graphics.TranslateTransform((float)inverseTranslateX, (float)inverseTranslateY);
            }

            // Draw the background image first.
            BackgroundDrawer(e);

            // Draw all the tanks, HP bars, names, and scores on the screen
            lock (TheWorld.GetTanks())
            {
                double tank_offset = (double)TheWorld.TANKWIDTH * 2.0 / 3.0;
                foreach (Tank t in TheWorld.GetTanks().Values)
                {
                    // Drawing the HP bar over the tanks
                    DrawObjectWithTransform(e, t, TheWorld.SIZE, t.location.GetX(), t.location.GetY() - tank_offset, 0, HPDrawer);
                    // Drawing the name and score under the tanks
                    DrawObjectWithTransform(e, t, TheWorld.SIZE, t.location.GetX(), t.location.GetY() + tank_offset, 0, TextDrawer);
                    // Drawing the actual tank body
                    DrawObjectWithTransform(e, t, TheWorld.SIZE, t.location.GetX(), t.location.GetY(), t.orientation.ToAngle(), PlayerDrawer);
                    // Drawing the tank turret.
                    DrawObjectWithTransform(e, t, TheWorld.SIZE, t.location.GetX(), t.location.GetY(), t.aiming.ToAngle(), TurretDrawer);
                }
            }

            // Draw all the projectiles on the screen
            lock (TheWorld.GetProjectiles())
            {
                foreach (Projectile p in TheWorld.GetProjectiles().Values)
                {
                    DrawObjectWithTransform(e, p, TheWorld.SIZE, p.location.GetX(), p.location.GetY(), p.orientation.ToAngle(), ProjectileDrawer);
                }
            }

            // Draw all the walls on the screen.
            lock (TheWorld.GetWalls())
            {
                foreach (Wall w in TheWorld.GetWalls().Values)
                {
                    // We have to loop through all the possitions of the walls.
                    foreach (Vector2D v in w.GetAllCoordinates(TheWorld.WALLWIDTH))
                    {
                        DrawObjectWithTransform(e, w, TheWorld.SIZE, v.GetX(), v.GetY(), 0, WallDrawer);
                    }
                }
            }

            //Draw all the powerups
            lock (TheWorld.GetPowerups())
            {
                foreach (Powerup p in TheWorld.GetPowerups().Values)
                {
                    DrawObjectWithTransform(e, p, TheWorld.SIZE, p.location.GetX(), p.location.GetY(), 0, PowerupDrawer);
                }
            }

            //Draw all the explosions
            lock (TheWorld.GetExplosions())
            {
                HashSet<int> ids = new HashSet<int>();
                foreach (Explosion ex in TheWorld.GetExplosions().Values)
                {
                    Image i = ex.DrawNextFrame();
                    if (i == null) ids.Add(ex.ID);
                    else DrawObjectWithTransform(e, i, TheWorld.SIZE, ex.location.GetX(), ex.location.GetY(), 0, ExplosionDrawer);
                }
                //remove explosions once they have been drawn
                foreach (int id in ids)
                {
                    TheWorld.RemoveGameObj(id, typeof(Explosion));
                }
            }

            //Draw all the beams
            lock (TheWorld.GetBeams())
            {
                HashSet<int> ids = new HashSet<int>();
                foreach (Beam b in TheWorld.GetBeams().Values)
                {
                    if (b.frame_count == 0) ids.Add(b.ID);
                    //Calculate the (x,y) of the beam to make it originate from the tip of the turret:
                    //add the origin (x,y) to half the turret size * the direction (x,y) 
                    double x = b.origin.GetX() + TheWorld.TURRSIZE / 2 * b.direction.GetX();
                    double y = b.origin.GetY() + TheWorld.TURRSIZE / 2 * b.direction.GetY();
                    DrawObjectWithTransform(e, b, TheWorld.SIZE, x, y, b.direction.ToAngle(), BeamDrawer);
                    b.frame_count--;
                }
                //Remove beams from the world once their frame count has been decremented
                foreach (int id in ids)
                {
                    TheWorld.RemoveGameObj(id, typeof(Beam));
                }
            }

            // Do anything that Panel (from which we inherit) needs to do
            base.OnPaint(e);
        }

    }
}