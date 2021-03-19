using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System.Linq;
using System;
using System.Collections.Generic;
using Elements.Geometry.Profiles;

namespace Noisewall
{
      public static class Noisewall
    {
        /// <summary>
        /// The Noisewall function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A NoisewallOutputs instance containing computed results and the model with any new elements.</returns>
        public static NoisewallOutputs Execute(Dictionary<string, Model> inputModels, NoisewallInputs input)
        {
            // Setup inputs
            var panelCentres = input.NoisewallSetoutCentres;
            var toleranceGap = input.ToleranceGap;
            var panelHeight = input.NoisewallPanelHeight;
            var panelDepth = input.NoisewallPanelDepth;
            var panelWidth = panelCentres - toleranceGap;
            var setoutPolylineCrv = input.SetoutCurve;

            // Model smooth setout crv
            var verts = setoutPolylineCrv.Vertices as List<Vector3>;
            var bezier = new Bezier(verts);
            var bezierModelCurve = new ModelCurve(bezier, new Material("Green", Colors.Green));

            // Divide curve
            var grid = new Grid1d(bezier);
            grid.DivideByFixedLength(panelCentres, FixedDivisionMode.RemainderAtBothEnds);
            var cells = grid.GetCells();
            var lines = cells.Select(c => c.GetCellGeometry()).OfType<Line>();

            List<Wall> walls = new List<Wall>();
            List<Beam> beams = new List<Beam>();

            
            // Create Beam profile
            var profile = WideFlangeProfileServer.Instance.GetProfileByType(WideFlangeProfileType.W10x100);
            
            // Model original Beam
            Line line = new Line(Vector3.Origin, new Vector3(0,0,panelHeight));
            List<Beam> linearBeams = new List<Beam>();


            
            foreach (var setoutLine in lines)
            {
                //  Factor in tolerance Gap using vector math
                var noisewallLength = panelCentres - toleranceGap * 2;
                Vector3 lineStartPoint = setoutLine.Start;
                Vector3 lineEndPoint = setoutLine.End;
                Vector3 lineDirectionUnitVector = (lineEndPoint - lineStartPoint).Unitized();
                var noisewallCentreline = new Line(lineStartPoint + lineDirectionUnitVector * toleranceGap, lineEndPoint - lineDirectionUnitVector * toleranceGap);

                // Create beam transforms
                Transform perpFrame = setoutLine.TransformAt(0);
                Transform centreSetoutPlane = setoutLine.TransformAt(0.5);
                Transform orientBeams = new Transform(setoutLine.Start, perpFrame.ZAxis, perpFrame.YAxis);

                // Model Beams
                var linearBeam = new Beam(line, profile, BuiltInMaterials.Steel,0,0,0, orientBeams);
                beams.Add(linearBeam);

                // Model Walls
                Material lightConcrete = new Material("Light Concrete", Colors.White, 0.1, 0.0);
                StandardWall wall = new StandardWall(noisewallCentreline, panelDepth, panelHeight, lightConcrete);
                walls.Add(wall);

            }

            // Create output object and add parameters
            var output = new NoisewallOutputs(walls.Count);

            // Add elements to output display
            output.Model.AddElement(bezierModelCurve);
            output.Model.AddElements(walls);
            output.Model.AddElements(beams);

            return output;
                
        }
    }
}