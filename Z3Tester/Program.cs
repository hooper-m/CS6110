using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;

namespace Z3Tester {
    class Program {
        static void Main(string[] args) {
            using (Context ctx = new Context()) {

                var a = ctx.MkBoolConst("a");

                var and = ctx.MkAnd(a, ctx.MkNot(a));
                Solver s = ctx.MkSolver();

                s.Assert(and);
                Console.WriteLine(s.Check());
                Console.Read();
            }
        }
    }
}
