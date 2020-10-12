using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Crossy
{
    public class MongoCRUD
    {
        //Variables
        private static MongoCRUD instance;
        private IMongoDatabase db;

        //Singleton
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
            //Creates a mongo client object
            var client = new MongoClient(DotNetEnv.Env.GetString("MONGO_TOKEN"));
            //If the client is not null (if it worked and connected)
            if (client != null)
            {
                //Write to console
                Console.WriteLine("Connected to mongo client");
            }
            //Connect to a certain DB
            db = client.GetDatabase("Crossy");
        }

        //This methd is to initiate a server inside the database
        public void InitServer<T>(T table)
        {
            //Get the servers collection from the database
            var collection = db.GetCollection<T>("Servers");
            //Insert the parsed table
            collection.InsertOne(table);
        }


        //This method is to load a server rec
        public List<T> LoadServerRec<T>(string id, string parsedFilter, string Collection)
        {
            //Get the parsed collection from the database
            var collection = db.GetCollection<T>(Collection);
            //Adds a filter
            var filter = Builders<T>.Filter.Eq(parsedFilter, id);

            //Returns a list of the found collections
            return collection.Find(filter).ToList();
        }

        //This method is for loading general records. Is interchangable with the method above.
        public List<T> LoadRecords<T>(string table)
        {
            //Get the servers collection from the database
            var collection = db.GetCollection<T>(table);

            //Return the found collection from the database as a list
            return collection.Find(new BsonDocument()).ToList();
        }

        //This method is for updating a record in the database
        public void UpdateRecord<T>(string table, string id, T record)
        {
            //Get the servers collection from the database
            var collection = db.GetCollection<T>(table);

            //Replace the object that has a matching ID and set upsert to true (update and insert)
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
