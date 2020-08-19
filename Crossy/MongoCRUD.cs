using System;
using MongoDB.Driver;

namespace Crossy
{
    public class MongoCRUD
    {
        private static MongoCRUD instance;
        private IMongoDatabase db;

        public static MongoCRUD Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MongoCRUD();
                }

                return instance;
            }
        }

        public MongoCRUD()
        {
            //DotNetEnv.Env.Load();

            var client = new MongoClient(DotNetEnv.Env.GetString("MONGO_TOKEN"));
            if(client != null)
            {
                Console.WriteLine("Connected to mongo client");
            }
            db = client.GetDatabase("Servers");
        }

        public void InitOrg<T>(T record)
        {
            var collection = db.GetCollection<T>("Servers");
            collection.InsertOne(record);
        }
    }
}
