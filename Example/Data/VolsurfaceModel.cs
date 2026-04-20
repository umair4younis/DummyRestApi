using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("441CCDD7-46BA-4bcc-A73C-718E78851ACD")]
    [ComVisible(true)]

    public class VolsurfaceModel
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public String FormulaReference { get; set; }

        public VolsurfaceModel()
        {
            Parameters = new List<VolsurfaceModelParameter>();
        }
        public void Add(VolsurfaceModelParameter p)
        {
            p.Model = this;
            Parameters.Add(p);
        }
        [ComVisible(false)]
        public IList<VolsurfaceModelParameter> Parameters { get; set; }

        public VolsurfaceModelParameters ParametersCollection
        {
            get
            {
                return new VolsurfaceModelParameters(Parameters);
            }
        }
    }

    public class FoVolsurfaceModel
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public String FormulaReference { get; set; }

        public FoVolsurfaceModel()
        {
            Parameters = new List<FoVolsurfaceModelParameter>();
        }
        public void Add(FoVolsurfaceModelParameter p)
        {
            p.Model = this;
            Parameters.Add(p);
        }
        [ComVisible(false)]
        public IList<FoVolsurfaceModelParameter> Parameters { get; set; }

        public FoVolsurfaceModelParameters ParametersCollection
        {
            get
            {
                return new FoVolsurfaceModelParameters(Parameters);
            }
        }

        public VolsurfaceModel Copy()
        {
            var volsurfaceModel = new VolsurfaceModel();
            volsurfaceModel.Id = Id;
            volsurfaceModel.Name = Name;
            volsurfaceModel.FormulaReference = FormulaReference;

            foreach (var parameter in Parameters)
            {
                volsurfaceModel.Parameters.Add(parameter.Copy(volsurfaceModel));
            }

            return volsurfaceModel;
            
        }
    }
}