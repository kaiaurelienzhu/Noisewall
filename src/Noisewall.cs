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
            var wallCentres = input.NoisewallSetoutCentres;
            var toleranceGap = input.ToleranceGap;
            double wallHeight = input.NoisewallPanelHeight;
            var wallDepth = input.NoisewallPanelDepth;
            var wallWidth = wallCentres - toleranceGap;
            var setoutPolylineCrv = input.SetoutCurve;
            var color = input.Color;



            // Model smooth setout crv
            var verts = setoutPolylineCrv.Vertices as List<Vector3>;
            var bezier = new Bezier(verts);
            var bezierModelCurve = new ModelCurve(bezier, new Material("Green", Colors.Green));

            // Divide curve
            var grid = new Grid1d(bezier);
            grid.DivideByFixedLength(wallCentres, FixedDivisionMode.RemainderAtBothEnds);
            var cells = grid.GetCells();
            var lines = cells.Select(c => c.GetCellGeometry()).OfType<Line>();
            int numWalls = lines.Count();

            List<Wall> walls = new List<Wall>();
            List<Beam> beams = new List<Beam>();

            
            // Create Beam profile
            var profile = WideFlangeProfileServer.Instance.GetProfileByType(WideFlangeProfileType.W10x100);
            
            // Model beam at origin
            Line line = new Line(Vector3.Origin, new Vector3(0,0,wallHeight));
            List<Beam> linearBeams = new List<Beam>();

            // Create range between 0 and 2pi with nDivisions
            List<double> normalisedRange = new List<double>();
            double max = 2 * Math.PI;
            double min = 0;
            int nDivisions = numWalls;
            double diff = (max - min)/nDivisions;
            double d = min;
            foreach(int i in Enumerable.Range(0, nDivisions - 1))
            {
                d = d + diff;
                normalisedRange.Add(min + d);
            }


            // Setup random heights within range
            int wallMinHeight = 6;
            int wallMaxHeight = 9;
            Random rand = new Random();
            List<int> randomWallHeights = new List<int>();
            foreach(int i in Enumerable.Range(0, numWalls))
            {
                randomWallHeights.Add(rand.Next(wallMinHeight, wallMaxHeight));
            }


            
            // // Base sin wave function parameters
            // List<double> normalisedHeights = new List<double>();
            // foreach (double number in normalisedRange)
            // {
            //     normalisedHeights.Add(Math.Sin(number));
            // }

            // // Remap heights
            // List<double> remappedHeights = new List<double>();
            // foreach (double number in normalisedHeights)
            // {
            //     remappedHeights.Add(Remap(number, min, max, wallMinHeight, wallMaxHeight));
            // }


            int increment = 0;
            foreach (var setoutLine in lines)
            {
                // Set wall 
                wallHeight = randomWallHeights.ElementAt(increment);
                //  Factor in tolerance Gap using vector math
                // panelHeight = remappedHeights.ElementAt(increment);
                wallHeight = input.NoisewallPanelHeight;
                

                var noisewallLength = wallCentres - toleranceGap * 2;
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
                StandardWall wall = new StandardWall(noisewallCentreline, wallDepth, wallHeight, lightConcrete);
                walls.Add(wall);
                increment ++;
            }

            // Create output object and add parameters
            var output = new NoisewallOutputs(walls.Count);

            // Add elements to output display
            output.Model.AddElement(bezierModelCurve);
            output.Model.AddElements(walls);
            output.Model.AddElements(beams);

            return output;
                
        }


        public static double Remap(double x, double from1, double from2, double to1, double to2)
        {
            return to2 + (x - from1) * (to2 - from1)/(from2 - from1);
        }
        
    }
    
}