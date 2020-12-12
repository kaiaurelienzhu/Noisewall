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

            var verts = setoutPolylineCrv.Vertices as List<Vector3>;
            var bezier = new Bezier(verts);
            var bezierModelCurve = new ModelCurve(bezier, new Material("Green", Colors.Green));

            // Experiment with Grid1d hypar elements
            var grid = new Grid1d(bezier);

            // Divide the grid into sections of length 10 and leave remainders at both ends
            grid.DivideByFixedLength(panelCentres, FixedDivisionMode.RemainderAtBothEnds);


            // Retrieve all bottom-level cells
            var cells = grid.GetCells();

            // Get lines representing each cell
            var lines = cells.Select(c => c.GetCellGeometry()).OfType<Line>();

            // Create walls from lines, and assign a random colour material
            List<Wall> walls = new List<Wall>();
            List<Beam> beams = new List<Beam>();
            var rand = new Random();
            
            // Create Beam profile
            var profile = WideFlangeProfileServer.Instance.GetProfileByType(WideFlangeProfileType.W10x100);
            
            
            foreach (var setoutLine in lines)
            {
                //  Factor in tolerance Gap using vector math
                var noisewallLength = panelCentres - toleranceGap * 2;
                Vector3 lineStartPoint = setoutLine.Start;
                Vector3 lineEndPoint = setoutLine.End;
                Vector3 lineDirectionUnitVector = (lineEndPoint - lineStartPoint).Unitized();
                var noisewallCentreline = new Line(lineStartPoint + lineDirectionUnitVector * toleranceGap, lineEndPoint - lineDirectionUnitVector * toleranceGap);

                // Define beam linework
                var line = new Line(lineStartPoint, new Vector3(lineStartPoint.X, lineStartPoint.Y, panelHeight));
                var linearBeam = new Beam(line, profile, BuiltInMaterials.Steel,0,0,45);
                var lineT = line.TransformAt(0).ToModelCurves(linearBeam.Transform);


                var colour = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), 1.0);
                walls.Add(new StandardWall(noisewallCentreline, panelDepth, panelHeight , new Material(colour, 0, 0, false, null, false, Guid.NewGuid(), colour.ToString())));
                beams.Add(linearBeam);
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