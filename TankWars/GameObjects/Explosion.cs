//Authors: Gavin Gray, Dan Ruley
//November, 2019
using System;
using System.Drawing;

namespace TankWars
{
    /// <summary>
    /// Explosion class to animate tanks explosive deaths. 
    /// 
    /// Must be initialized with a location Vector2D, ID (that of the dying tank), and an animatable Image (.GIF).
    /// The animation will run for the duration of it's frames*2, and when it's done animating it will return a NULL Image 
    /// on DrawNextFrame.
    /// </summary>
    public class Explosion
    {
        public Image gif { private set; get; }
        private bool currentlyAnimating = false;
        private readonly int FRAME_COUNT;
        public int ID { private set; get; }
        private int counter;
        public Vector2D location { private set; get; }

        /// <summary>
        /// Create an Explosion object with an Image, ID, and Location 
        /// </summary>
        /// <param name="i">Animatable Image to be used as the GIF.</param>
        /// <param name="loc">Location that the Explosion should be drawn</param>
        /// <param name="id">ID of the dying tank</param>
        public Explosion(Image i, Vector2D loc, int id)
        {
            gif = i;
            location = loc;
            System.Drawing.Imaging.FrameDimension FrameDimensions = new System.Drawing.Imaging.FrameDimension(i.FrameDimensionsList[0]);
            FRAME_COUNT = gif.GetFrameCount(FrameDimensions);
            counter = 0;
            ID = id;
        }

        /// <summary>
        /// Starts the animation of the image if it hasn't started already. 
        ///     it is important that an image is not tried to be started twice.
        /// </summary>
        private void AnimateImage()
        {
            if (!currentlyAnimating)
            {
                ImageAnimator.Animate(gif, new EventHandler(this.frameChanged));
                currentlyAnimating = true;
            }
        }

        /// <summary>
        /// DrawNextFrame will return the current Image of the GIF to be drawn, and when it is done
        ///     will return NULL indicating that the GIF is done running.
        /// </summary>
        /// <returns></returns>
        public Image DrawNextFrame()
        {
            // If we've reached the max amount of frames we will destroy the object, and indeicate to the user that it cannot be used anymore by returning NULL
            if (counter >= FRAME_COUNT * 2)
            {
                Destroy();
                return null;
            }
            AnimateImage();
            // Increase count of the frames that have been run
            counter++;
            // Prepare the next frame of the animation
            ImageAnimator.UpdateFrames();
            // Return the current frame to be displayed.
            return this.gif;
        }

        /// <summary>
        ///  Destroy will stop animation for the current image, dispose of the GIF, and set the field 'gif' to null.
        /// </summary>
        private void Destroy()
        {
            if (gif != null)
            {
                if (currentlyAnimating) ImageAnimator.StopAnimate(gif, this.frameChanged);
            }
            currentlyAnimating = false;
            gif.Dispose();
            gif = null;
        }

        /// <summary>
        /// frameChanged is the callBack for the start animation object, however, we don't want it to do anything because 
        ///     the DrawingPanel will keep track of when to draw the next frame.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void frameChanged(Object o, EventArgs e) { }

    }
}
