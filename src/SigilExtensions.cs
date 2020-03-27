using System;

namespace SimpleECS
{
    static class SigilExtensions
    {
        public static void For<T>(this Sigil.Emit<T> il, Sigil.Local length, Action<Sigil.Local> body)
        {
            var i = il.DeclareLocal<int>("i");
            il.LoadConstant(0);
            il.StoreLocal(i);

            var startLoop = il.DefineLabel("start");
            var endLoop = il.DefineLabel("end");

            il.MarkLabel(startLoop);
            il.LoadLocal(i);
            il.LoadLocal(length);
            il.BranchIfGreaterOrEqual(endLoop);

            body(i);

            il.LoadLocal(i);
            il.LoadConstant(1);
            il.Add();
            il.StoreLocal(i);

            il.Branch(startLoop);
            il.MarkLabel(endLoop);
        }
    }
}
