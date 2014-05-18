using System;

namespace XNADriver
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (BaseGame game = new BaseGame())
            {
                game.Run();
            }
        }
    }
#endif
}

