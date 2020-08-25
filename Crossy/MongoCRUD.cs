using System;
using System.Collections.Generic;
using MongoDB.Bson;
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
            var client = new MongoClient(DotNetEnv.Env.GetString("MONGO_TOKEN"));
            if(client != null)
            {
                Console.WriteLine("Connected to mongo client");
            }
            db = client.GetDatabase("Crossy");
        }

        public void InitServer<T>(T table)
        {
            var collection = db.GetCollection<T>("Servers");
            collection.InsertOne(table);
        }

        public List<T> LoadServerRec<T>(string id, string parsedFilter, string Collection)
        {
            var collection = db.GetCollection<T>(Collection);
            var filter = Builders<T>.Filter.Eq(parsedFilter, id);

            return collection.Find(filter).ToList();
        }

        public List<T> LoadRecords<T>(string table)
        {       
            var collection = db.GetCollection<T>(table);

            return collection.Find(new BsonDocument()).ToList();
        }

        public void UpdateWarning<UserWarning>(string table, string id, UserWarning record)
        {
            var collection = db.GetCollection<UserWarning>(table);

            var result = collection.ReplaceOne(
                new BsonDocument("_id", id),
                record,
                new ReplaceOptions
                {
                    IsUpsert = true
                });
        }
    }
}
