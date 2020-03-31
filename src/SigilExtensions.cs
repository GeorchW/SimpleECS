using System.Linq.Expressions;
using System;
using System.Reflection;

namespace SimpleECS
{
    static class SigilExtensions
    {
        public static void For<T>(this Sigil.Emit<T> il, Action getStart, Action getEnd, Action<Sigil.Local> body)
        {
            var i = il.DeclareLocal<int>("i");
            getStart();
            il.StoreLocal(i);

            var startLoop = il.DefineLabel("start");
            var endLoop = il.DefineLabel("end");

            il.MarkLabel(startLoop);
            il.LoadLocal(i);
            getEnd();
            il.BranchIfGreaterOrEqual(endLoop);

            body(i);

            il.LoadLocal(i);
            il.LoadConstant(1);
            il.Add();
            il.StoreLocal(i);

            il.Branch(startLoop);
            il.MarkLabel(endLoop);
        }

        public static Sigil.Local StoreInNewLocal<TEmit, TLocal>(this Sigil.Emit<TEmit> il, Expression<Func<TLocal>> propOrCall, string? name = null)
        {
            if (propOrCall.Body is MethodCallExpression call)
                il.Call(call.Method);
            else if(propOrCall.Body is MemberExpression member && member.Member is PropertyInfo prop)
                il.Call(prop.GetMethod);
            else
                throw new NotImplementedException($"The expression \"{propOrCall.Body}\" (type: {propOrCall.Body.NodeType}) is not valid as a property or call.");
            var local = il.DeclareLocal<TLocal>(name);
            il.StoreLocal(local);
            return local;
        }
    }
}
