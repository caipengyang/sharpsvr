using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace sharpsvr.proxy
{
    public class ProxyGenerator
    {

        public static T Of<T>(Interceptor proxy) where T : class
        {
            var assemblyName = new AssemblyName(typeof(T).Name + "ProxyAssembly");
            assemblyName.Version = new Version("1.0.0");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assembly.DefineDynamicModule(typeof(T).Name + "ProxyModule");
            var typeBuilder = moduleBuilder.DefineType(typeof(T).Name + "Proxy", TypeAttributes.Public, typeof(object), new Type[] { typeof(T) });
            var fieldInterceptor = typeBuilder.DefineField("_interceptor", typeof(Interceptor), attributes: FieldAttributes.Public);
            InjectInterceptor<T>(typeBuilder, fieldInterceptor, proxy);
            var t = typeBuilder.CreateType();
            var obj = Activator.CreateInstance(t) as T;
            var fieldInfo = t.GetField("_interceptor");
            fieldInfo.SetValue(obj, proxy);
            return obj;
        }

        private static void InjectInterceptor<T>(TypeBuilder typeBuilder, FieldBuilder fieldInterceptor, Interceptor proxy)
        {
            //define constructors.
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            var ilOfCtor = constructorBuilder.GetILGenerator();

            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Ldnull);
            ilOfCtor.Emit(OpCodes.Stfld, fieldInterceptor);
            ilOfCtor.Emit(OpCodes.Ret);

            //define methods.
            var methodInfos = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methodInfos)
            {
                var methodParameterTypes =
                method.GetParameters().Select(p => p.ParameterType).ToArray();

                MethodBuilder methodBuilder = null;
                if (method.ContainsGenericParameters)
                {
                    methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual, method.ReturnType,
                        methodParameterTypes);
                    methodBuilder.SetReturnType(method.ReturnType);
                    GenericTypeParameterBuilder[] genericParams = methodBuilder.DefineGenericParameters(method.GetGenericArguments().Select(p => p.Name).ToArray());
                }
                else
                {
                    methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, method.ReturnType,
                        methodParameterTypes);
                }

                typeBuilder.DefineMethodOverride(methodBuilder, method);
                var ilOfMethod = methodBuilder.GetILGenerator();
                ilOfMethod.Emit(OpCodes.Ldarg_0);
                ilOfMethod.Emit(OpCodes.Ldfld, fieldInterceptor);
                var methodInfoLocal = ilOfMethod.DeclareLocal(typeof(MethodInfo));
                ilOfMethod.Emit(OpCodes.Ldtoken, method);
                ilOfMethod.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) }));
                ilOfMethod.Emit(OpCodes.Stloc, methodInfoLocal);
                ilOfMethod.Emit(OpCodes.Ldloc, methodInfoLocal);

                if (methodParameterTypes == null || methodParameterTypes.Length == 0)
                {
                    ilOfMethod.Emit(OpCodes.Ldnull);
                }
                else
                {
                    var parameters = ilOfMethod.DeclareLocal(typeof(object[]));
                    ilOfMethod.Emit(OpCodes.Ldc_I4_S, methodParameterTypes.Length);
                    ilOfMethod.Emit(OpCodes.Newarr, typeof(object));
                    ilOfMethod.Emit(OpCodes.Stloc, parameters);
                    for (var j = 0; j < methodParameterTypes.Length; j++)
                    {
                        ilOfMethod.Emit(OpCodes.Ldloc, parameters);
                        ilOfMethod.Emit(OpCodes.Ldc_I4, j);
                        ilOfMethod.Emit(OpCodes.Ldarg, j + 1);
                        if (methodParameterTypes[j].IsPrimitive || methodParameterTypes[j].IsValueType || methodParameterTypes[j].ContainsGenericParameters) ilOfMethod.Emit(OpCodes.Box, methodParameterTypes[j]);
                        ilOfMethod.Emit(OpCodes.Stelem_Ref);
                    }
                    ilOfMethod.Emit(OpCodes.Ldloc, parameters);
                }
                // call Invoke() method of Interceptor
                ilOfMethod.Emit(OpCodes.Callvirt, proxy.GetType().GetMethod("Invoke"));

                // pop the stack if return void
                if (method.ReturnType == typeof(void))
                {
                    ilOfMethod.Emit(OpCodes.Pop);
                }
                else
                {
                    if (method.ReturnType.IsPrimitive || method.ReturnType.IsValueType) ilOfMethod.Emit(OpCodes.Unbox_Any, method.ReturnType);
                }
                ilOfMethod.Emit(OpCodes.Ret);
            }
        }
    }
}