﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents ta reduce expression.
    /// This transforms ta list of N items into ta list of 1 items (unless N == 0) by 
    /// applying an associative binary function to items in the list. 
    /// </summary>
    public class ReduceExpr : Expression
    {
        [HeronVisible] public string a;
        [HeronVisible] public string b;
        [HeronVisible] public Expression list;
        [HeronVisible] public Expression expr;

        public ReduceExpr(string a, string b, Expression list, Expression expr)
        {
            this.a = a;
            this.b = b;
            this.list = list;
            this.expr = expr;
        }

        public override HeronValue Eval(VM vm)
        {
            SeqValue seq = vm.EvalList(list);
            List<HeronValue> input = new List<HeronValue>(seq.ToDotNetEnumerable(vm));
            HeronValue[] output = Eval_MultiThreaded(vm, input.ToArray()).ToArray();
            return new ListValue(output as System.Collections.Generic.IEnumerable<HeronValue>);
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return this;
        }

        private void ReduceArray(VM vm, HeronValue[] input, int begin, int cnt)
        {
            OptimizationParams op = new OptimizationParams();
            Accessor A = op.AddNewAccessor(a);
            Accessor B = op.AddNewAccessor(b);
            Expression X = expr.Optimize(op);

            while (cnt > 1)
            {
                int cur = begin;

                for (int i = 0; i < cnt - 1; i += 2)
                {
                    A.Set(input[begin + i]);
                    B.Set(input[begin + i + 1]);
                    input[cur++] = vm.Eval(X);
                }

                if (cnt % 2 == 1)
                {
                    input[cur++] = input[begin + cnt - 1];
                }

                int r = cur - begin;
                Debug.Assert(r >= 1);
                Debug.Assert(r < cnt);
                cnt = r;
            }
        }

        public void ReduceArrayTask(VM vm, HeronValue[] input, HeronValue[] output, int i, int tasks)
        {
            int cnt = input.Length / tasks;
            if (input.Length % tasks > 0)
                cnt += 1;
            int begin = i * cnt;
            if (begin + cnt > input.Length)
                cnt = input.Length - begin;

            ReduceArray(vm, input, begin, cnt);
            output[i] = input[begin];
        }

        public List<HeronValue> Eval_MultiThreaded(VM vm, HeronValue[] input)
        {
            int nTasks = Config.maxThreads;
            if (input.Length < nTasks * 4)
                nTasks = 1; 

            HeronValue[] output = new HeronValue[nTasks];
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = nTasks;
            Parallel.For(0, nTasks, po, (int i) => ReduceArrayTask(vm.Fork(), input, output, i, nTasks));
            ReduceArray(vm, output, 0, nTasks);
            return new List<HeronValue>() { output[0] };
        }

        public override string ToString()
        {
            return "reduce (" + a + ", " + b + " in " + list.ToString() + ") " + expr.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ReduceExpr;
        }
    }
}