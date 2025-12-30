namespace Klacks.Api.Infrastructure.Scripting
{
    public class Scope
    {
        private List<Identifier> Variables = new();


        public Identifier Allocate(string name, object? value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.IdVariable)
        {
            Identifier id = new Identifier()
            {
                Name = name,
                Value = value,
                IdType = idType
            };


            SetVariable(id, name);

            return id;
        }


        public void Assign(string name, object value)
        {
            SetVariable(value, name);
        }


        public object Retrieve(string name)
        {
            return GetVariable(name)!;
        }


        public bool Exists(string name)
        {
            return GetVariable(name) != null;
        }


        public object? GetVariable(string name)
        {
            try
            {
                if (Variables != null)
                {
                    return Variables.Where(x => x.Name == name).FirstOrDefault()!;
                }
                else
                {
                    return null;
                }


            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SetVariable(object value, string name)
        {
            // Benannten Wert löschen , damit es ersetzt wird
            if (Variables.Count > 0)
            {
                var tmp = Variables.Where(x => x != null && x.Name == name).FirstOrDefault();
                if (tmp != null) { Variables.Remove(tmp); }
            }

            // Variablen immer am Anfang des Scopes zusammenhalten. Nach der letzten
            // Variablen kommen nur noch echte Stackwerte

            Identifier c;
            if (value is Identifier existingId)
            {
                c = existingId;
            }
            else
            {
                c = new Identifier { Name = name, Value = value };
            }

            if (Variables.Count == 0) { Variables.Add(c); }
            else { Variables.Insert(0, c); }

        }


        public void Push(Identifier value)
        {
            Variables.Add(value);
        }


        ///  Holt den obersten unbenannten Wert vom Stack.
        ///  Wenn eine index übergeben wird, kann auch auf Stackwerte
        ///  direkt zugegriffen werden. In dem Fall werden sie nicht
        ///  gelöscht! Index: 0..n; 0=oberster Stackwert, 1=darunterliegender usw.
        public Identifier Pop(int index = -1)
        {
            Identifier? pop = null;
            if (index < 0)
            {
                // Den obersten Stackwert vom Stack nehmen und zurückliefern
                // Die Stackwerte fangen nach der letzten benannten Variablen im Scope an

                if (Variables.Count > 0)
                {
                    pop = Variables[Variables.Count - 1];
                    Variables.Remove(pop);
                }
            }
            else
            {
                pop = Variables[Variables.Count - 1 - index];
            }

            return pop!;
        }

        internal int CloneCount()
        {
            return Variables.Count;
        }

        public object CloneItem(int index)
        {
            return Variables[index];
        }
    }

}
