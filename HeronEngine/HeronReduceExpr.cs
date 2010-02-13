using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public override Expression Optimize(VM vm)
        {
            return this;
        }

        private void ReduceArray(VM vm, HeronValue[] input, int begin, int cnt)
        {
            using (vm.CreateFrame())
            {
                vm.AddVar(a, HeronValue.Null);
                vm.AddVar(b, HeronValue.Null);
                VM.Accessor A = vm.GetAccessor(a);
                VM.Accessor B = vm.GetAccessor(b);

                Expression X = expr;
                if (Config.optimize)
                    X = expr.Optimize(vm);

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
        }

        public Task CreateReduceArrayTask(VM vm, HeronValue[] input, HeronValue[] output, int begin, int cnt, int id)
        {
            // Make sure we don't go out of range
            if (begin + cnt > input.Length)
                cnt = input.Length - begin;

            return () =>
            {
                ReduceArray(vm, input, begin, cnt);
                output[id] = input[begin];
            };
        }

        public List<HeronValue> Eval_MultiThreaded(VM vm, HeronValue[] input)
        {
            if (input.Length < 100 || Config.maxThreads < 2)
            {
                ReduceArray(vm, input, 0, input.Length);
                if (input.Length > 0)
                    return new List<HeronValue>() { input[0] };
                else
                    return new List<HeronValue>();
            }
            else
            {
                int nTasks = Config.maxThreads;
                HeronValue[] output = new HeronValue[nTasks];
                int cnt = input.Length / nTasks;
                if (input.Length % nTasks > 0)
                    cnt += 1;
                List<Task> tasks = new List<Task>();
                int begin = 0;
                VM vm_tmp = vm;
                for (int i = 0; i < nTasks; ++i)
                {
                    Task t = CreateReduceArrayTask(vm_tmp, input, output, begin, cnt, i);
                    vm_tmp = vm_tmp.Fork();
                    tasks.Add(t);
                    begin += cnt;
                }
                Parallelizer.DistributeWork(tasks);
                ReduceArray(vm, output, 0, nTasks);
                return new List<HeronValue>() { output[0] };
            }
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