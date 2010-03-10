using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace HeronEngine
{
    /// <summary>
    /// Represents an expression involving the "select" operator
    /// which filters a list depending on a predicate.
    /// </summary>
    public class SelectExpr : Expression
    {
        [HeronVisible] public string name;
        [HeronVisible] public Expression list;
        [HeronVisible] public Expression pred;

        public SelectExpr(string name, Expression list, Expression pred)
        {
            this.name = name;
            this.list = list;
            this.pred = pred;
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return this;
        }

        public override HeronValue Eval(VM vm)
        {
            IInternalIndexable ii = vm.EvalInternalList(list);
            bool[] bools = new bool[ii.InternalCount()];

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
                    lp.expr = pred.Optimize(lp.op);
                    return lp;
                },
                (Tuple<int, int> range, ParallelLoopState state, LoopParams lp) =>
                {
                    for (int i = range.Item1; i < range.Item2; ++i)
                    {
                        lp.acc.Set(ii.InternalAt(i));
                        bools[i] = lp.vm.Eval(lp.expr).ToBool();
                    }
                    return lp;
                },
                (LoopParams lp) => { }
                );

            List<HeronValue> r = new List<HeronValue>(ii.InternalCount());
            for (int i = 0; i < ii.InternalCount(); ++i)
                if (bools[i])
                    r.Add(ii.InternalAt(i));
            r.Capacity = r.Count;
            return new ListValue(r);
        }

        public override string ToString()
        {
            return "select (" + name + " from " + list.ToString() + ") " + pred.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SelectExpr;
        }
    }
}
