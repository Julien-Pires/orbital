﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Orbital.Data
{
    internal sealed class AppDomainDescription : BaseDescription
    {
        #region Fields

        private const BindingFlags FieldsFlag = BindingFlags.Instance | BindingFlags.Public;

        private readonly Dictionary<string, AssemblyDescription> _assemblies =
            new Dictionary<string, AssemblyDescription>();

        #endregion

        #region Constructors

        internal AppDomainDescription() : base(Application.productName)
        {
        }

        #endregion

        #region Assembly Methods

        private AssemblyDescription EnsureAssembly(string name)
        {
            AssemblyDescription assembly;
            if (!_assemblies.TryGetValue(name, out assembly))
            {
                assembly = new AssemblyDescription(name);
                _assemblies.Add(name, assembly);
            }

            return assembly;
        }

        internal AssemblyDescription CreateFromSchema(Schema schema)
        {
            if (schema == null)
                throw new ArgumentNullException("schema");

            ObjectSchema[] objSchemas = schema.Objects;
            AssemblyDescription assembly = new AssemblyDescription(schema.Namespace);
            for (int i = 0; i < objSchemas.Length; i++)
            {
                ObjectSchema currObjectSchema = objSchemas[i];
                TypeDescription typeDescription = CreateTypeFromSchema(currObjectSchema);
                assembly.RegisterType(currObjectSchema.Namespace, typeDescription);
            }

            return assembly;
        }

        #endregion

        #region Type Methods

        public TypeDescription GetType(string assemblyName, string fullname)
        {
            string namespaceName, name;
            DescriptionHelper.ExtractNames(fullname, out namespaceName, out name);

            return EnsureAssembly(assemblyName).GetType(namespaceName, name);
        }

        public TypeDescription RegisterType(Type type)
        {
            return RegisterTypeFromClrType(type);
        }

        #endregion

        #region Schema Methods

        private TypeDescription CreateType(string name, TypeKind kind, object parameters = null)
        {
            TypeDescription type = TypeDescription.GetPrimitiveType(kind);
            if (type != null)
                return type;

            switch (kind)
            {
                case TypeKind.Enum:
                    type = new EnumDescription(name, (string[])parameters);
                    break;

                case TypeKind.Class:
                case TypeKind.Struct:
                    type = new ObjectDescription(name, kind);
                    break;

                case TypeKind.Array:
                case TypeKind.List:
                case TypeKind.Dictionary:
                    type = new CollectionDescription(name, kind);
                    break;
            }

            if (type == null)
                throw new InvalidOperationException(string.Format("{0} is not a supported type", kind));

            return type;
        }

        private TypeDescription CreateTypeFromSchema(ObjectSchema objectSchema)
        {
            if (objectSchema == null)
                throw new ArgumentNullException("objectSchema");

            return CreateType(objectSchema.Name, objectSchema.Kind, objectSchema.Values);
        }

        #endregion

        #region Reflection Methods

        private TypeDescription RegisterTypeFromClrType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            AssemblyDescription assembly = EnsureAssembly(type.Assembly.GetName().Name);
            if (type.IsPrimitive)
                assembly.RegisterType(type.Namespace, TypeDescription.GetPrimitiveType(type));
            else if (type.IsEnum)
                assembly.RegisterType(type.Namespace, new EnumDescription(type.Name, Enum.GetNames(type)));
            else if (type.IsClass || type.IsValueType)
            {
                TypeKind objKind = type.IsClass ? TypeKind.Class : TypeKind.Struct;
                ObjectDescription objDescription = new ObjectDescription(type.Name, objKind);
                assembly.RegisterType(type.Namespace, objDescription);

                ExtractObjectProperties(objDescription, type);
            }

            return assembly.GetType(type.Namespace, type.Name);
        }

        private TypeDescription GetOrCreateType(Type type)
        {
            string assemblyName = type.Assembly.GetName().Name;

            return GetType(assemblyName, type.FullName) ?? RegisterTypeFromClrType(type);
        }

        private void ExtractObjectProperties(ObjectDescription objDescription, Type type)
        {
            PropertyInfo[] properties = type.GetProperties(FieldsFlag);
            for (int i = 0; i < properties.Length; i++)
            {
                if(!properties[i].CanRead || !properties[i].CanWrite)
                    continue;

                if(properties[i].GetIndexParameters().Length > 0)
                    continue;

                TypeDescription typeDescription = GetOrCreateType(properties[i].PropertyType);
                PropertyDescription propertyDescription = new PropertyDescription(properties[i].Name)
                {
                    TypeDescription = typeDescription
                };

                objDescription[propertyDescription.Name] = propertyDescription;
            }

            FieldInfo[] fields = type.GetFields(FieldsFlag);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsInitOnly)
                    continue;

                TypeDescription typeDescription = GetOrCreateType(fields[i].FieldType);
                PropertyDescription propertyDescription = new PropertyDescription(fields[i].Name)
                {
                    TypeDescription = typeDescription
                };

                objDescription[propertyDescription.Name] = propertyDescription;
            }
        }

        #endregion
    }
}