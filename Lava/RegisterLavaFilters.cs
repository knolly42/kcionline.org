using DotLiquid;
using Rock.Utility;

namespace org.kcionline.bricksandmortarstudio.Lava
{
    class RegisterLavaFilters : IRockStartup
    {
        public int StartupOrder
        {
            get
            {
                return 0;
            }
        }

        public void OnStartup()
        {
            Template.RegisterFilter( typeof( org.kcionline.bricksandmortarstudio.Lava.LavaFilters) );
        }
    }
}
