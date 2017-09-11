using System;

namespace commontest.src.test
{
    [Serializable]
    public class User
    {
        public long Id{get;set;}
        public int Age{get;set;}
        public bool Sex{get;set;}
        public string Message{get;set;}
        public User Child{get;set;}
        
        public override string ToString(){
            return $"Id={Id}, Age={Age}, Sex={Sex}, Message={Message}, Child={Child}";
        }
    }
}