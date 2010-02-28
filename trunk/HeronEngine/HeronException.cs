using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HeronEngine
{
    public class VMException : Exception
    {
        FunctionDefn defn;
        Statement st;
        string filename;

        public VMException(FunctionDefn defn, string filename, Statement st, Exception inner)
            : base("", inner)
        {
            this.defn = defn;
            this.st = st;
            this.filename = filename;
        }

        public Exception InnerMostException
        {
            get
            {
                Exception e = this;
                while (e.InnerException != null)
                    e = e.InnerException;
                return e;
            }
        }

        public void OutputLocation()
        {
            VMException e = InnerException as VMException;
            if (e != null)
                e.OutputLocation();
            
            if (defn != null)
            {
                if (st != null && st.node != null)
                {
                    Console.Write(" at line " + (st.node.CurrentLineIndex + 1).ToString());
                }
                else if (defn.node != null)
                {
                    Console.Write(" at line " + (defn.node.CurrentLineIndex + 1).ToString());
                }
                Console.Write(" of file " + Path.GetFileName(defn.FileName));
                Console.Write(" in function " + defn.GetSignature());
                Console.WriteLine();
            }
        }

        public void OutputErrorMessage()
        {
            Console.WriteLine("Error occured '" + InnerMostException.Message + "'");
            OutputLocation();
        }
    }
}
