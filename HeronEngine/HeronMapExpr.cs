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
    /// Represents ta map expression.
    /// This transforms ta list of N items into ta list of N items by 
    /// applying ta unary function to each item in the list.
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
         
        public override HeronValue Eval(VM vm)
        {   
            IInternalIndexable ii = vm.EvalInternalList(list);
            HeronValue[] output = new HeronValue[ii.InternalCount()];
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Config.maxThreads;
            var p = Partitioner.Create(0, ii.InternalCount());
            Parallel.ForEach(
                p,
                po,
                () =>
                {
                    LoopParams lp = new LoopParams();
                    lp.op = new OptimizationParams();
                    lp.acc = lp.op.AddNewAccessor(name);
                    lp.vm = vm.Fork();
                    lp.expr = yield.Optimize(lp.op);
                    return lp;
                },
                (Tuple<int, int> range, ParallelLoopState state, LoopParams lp) =>
                {
                    for (int i = range.Item1; i < range.Item2; ++i)
                    {
                        lp.acc.Set(ii.InternalAt(i));
                        output[i] = lp.vm.Eval(lp.expr);
                    }
                    return lp;
                },
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