﻿using System;
using System.Collections.Generic;
using System.Linq;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Core
{
    public class Scope
    {
        internal readonly NormalizedScopeEntryHash _hash = new NormalizedScopeEntryHash();
        internal readonly VariableCollection VariableCollection = new VariableCollection();

        public readonly Stack<object> Stack = new Stack<object>();
        public Scope Parent { get; set; }

        public Scope Begin()
        {
            return new Scope() { Parent = this };
        }

        public bool IsGlobal()
        {
            return Parent == null;
        }

        public Scope End()
        {
            return Parent;
        }

        public object GetVar(string varName)
        {
            if (VariableCollection.Exists(varName))
            {
                return VariableCollection[varName].Value;
            }
            else if (Parent != null)
            {
                return Parent.GetVar(varName);
            }

            throw new Exception($"Variable {varName} was not declared.");
        }

        public void SetVar(string varName, object varValue, bool forceOnTop = false, Type declarationType = null)
        {
            Scope current = this;
            bool found = false;

            while (!found)
            {
                bool exists = current.VariableCollection.Exists(varName);

                if (current != null && (exists || forceOnTop))
                {
                    found = true;

                    if (!exists)
                    {
                        if (declarationType != null)
                        {
                            VariableEntry entry;

                            if (varValue != null && varValue.GetType() != declarationType)
                            {
                                entry = new VariableEntry(varName, declarationType, Convert.ChangeType(varValue, declarationType));
                            }
                            else
                            {
                                entry = new VariableEntry(varName, declarationType, varValue);
                            }

                            current.VariableCollection.Add(entry);
                        }
                        else
                        {
                            throw new Exception($"Variable {varName} is not declared.");
                        }
                    }
                    else
                    {
                        current.VariableCollection[varName].SetValue(varValue);
                    }
                }
                else if (current == null)
                {
                    break;
                }

                current = current.Parent;
            }

            if (!found)
            {
                throw new Exception($"Variable {varName} is not declared.");
            }
        }

        public NormalizedScopeEntry[] GetNormalizedScopeEntries(XppProxy proxy = null)
        {
            List<NormalizedScopeEntry> ret = new List<NormalizedScopeEntry>();

            foreach (VariableEntry variable in VariableCollection)
            {
                NormalizedScopeEntry entry = new NormalizedScopeEntry()
                {
                    VariableName = variable.Name,
                    Value = variable.Value,
                    TypeName = variable.DeclarationType is null ? variable.Value?.GetType().FullName ?? "" : variable.DeclarationType.FullName,
                };

                if (proxy != null && proxy.Reflection.IsEnum(entry.TypeName))
                {
                    entry.EnumValues = proxy.Reflection.GetAllEnumValues(entry.TypeName).Cast<string>().ToArray();
                }

                entry.Changed = _hash.HasChanged(entry.VariableName, entry.Value);

                ret.Add(entry);
            }

            _hash.Update(ret);

            return ret.ToArray();
        }
    }
}
