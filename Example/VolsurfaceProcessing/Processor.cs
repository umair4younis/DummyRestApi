using Puma.MDE.Data;

namespace VolsurfaceProcessing
{
    public class Processor
    {
        private Underlying und;
        private VolsurfaceModel defaultmodel;

        public Processor(Underlying und)
        {
            this.und = und;
        }

        public Processor(Underlying und, VolsurfaceModel defaultmodel)
        {
            this.und = und;
            this.defaultmodel = defaultmodel;
        }
    }
}