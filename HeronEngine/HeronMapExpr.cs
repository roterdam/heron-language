using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Parallel;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace HeronEngine
{
    /// <summary>
    /// Represents a map expression.
    /// This transforms a list of N items into a list of N items by 
    /// applying a unary function to each item in the list.
    /// </summary>
    public class MapExpr : Expression
    {
        [HeronVisible] public string name;
        [HeronVisible] public Expression list;
        [HeronVisible] public Expression yield;

        public MapExpr(string name, Expression list, Expression yield)
        {
            this.name = name;
            this.list = list;
            this.yield = yield;
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return this;
        }
         
        /// <summary>
        /// Evaluates the map operation 
        /// </summary>
        /// <param name="vm">Current state of virtual machine</param>
        /// <returns>An ArrayValue containing new values</returns>
        public override HeronValue Eval(VM vm)
        {
            SeqValue sv = vm.Eval(list) as SeqValue;
            if (sv == null)
                throw new Exception("Expected list: " + list.ToString());

            // internal structure for indexing lists
            IInternalIndexable ii = sv.GetIndexable();
            if (ii.InternalCount() == 0)
                return sv;

            // Array of values used for output of map operations
            HeronValue[] output = new HeronValue[ii.InternalCount()];
            
            // Create a parallel options object to limit parallelism
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Config.maxThreads;
            
            // Create a partitioner
            var partitioner = Partitioner.Create(0, ii.InternalCount());
            
            Parallel.ForEach(
                // Breaks the for loop up into sub-ranges
                partitioner, 
                // Parellel options
                po,
                // Initialization of thread-local variables
                () => 
                {  
                    LoopParams lp = new LoopParams();
                    lp.op = new OptimizationParams();
                    lp.acc = lp.op.AddNewAccessor(name);
                    lp.vm = vm.Fork();
                    lp.expr = yield.Optimize(lp.op);
                    return lp;
                },
                // Loop body
                (Tuple<int, int> range, ParallelLoopState state, LoopParams lp) =>
                {
                    for (int i = range.Item1; i < range.Item2; ++i)
                    {
                        lp.acc.Set(ii.InternalAt(i));
                        output[i] = lp.vm.Eval(lp.expr);
                    }
                    return lp;
                },
                // Finalization function
                (LoopParams lp) => { }
                );

            return new ArrayValue(output);
        }

        public override string ToString()
        {           
            return "map (" + name + " in " + list.ToString() + ") to " + yield.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.MapExpr;
        }
    }
}