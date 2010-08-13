using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Configuration;
using MongoDB.Connections;
using MongoDB.Protocol;
using MongoDB.Results;
using MongoDB.Util;
using MongoDB.Configuration.Mapping.Model;
using System.Collections;

namespace MongoDB
{
    /// <summary>
    /// 
    /// </summary>
    public class MongoCollection<T> : IMongoCollection<T>, IMongoCollection where T : class
    {
        private readonly MongoConfiguration _configuration;
        private readonly Connection _connection;
        private MongoDatabase _database;
        private CollectionMetadata _metadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="collectionName">The name.</param>
        internal MongoCollection(MongoConfiguration configuration, Connection connection, string databaseName, string collectionName)
        {
            //Todo: add public constructors for users to call
            Name = collectionName;
            DatabaseName = databaseName;
            _configuration = configuration;
            _connection = connection;
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>The database.</value>
        public IMongoDatabase Database
        {
            get { return _database ?? (_database = new MongoDatabase(_configuration, _connection, DatabaseName)); }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        /// <value>The name of the database.</value>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Gets the full name including database name.
        /// </summary>
        /// <value>The full name.</value>
        public string FullName
        {
            get { return DatabaseName + "." + Name; }
        }

        /// <summary>
        /// Gets the meta data.
        /// </summary>
        /// <value>The meta data.</value>
        public CollectionMetadata Metadata
        {
            get { return _metadata ?? (_metadata = new CollectionMetadata(_configuration, DatabaseName, Name, _connection)); }
        }

        /// <summary>
        /// Finds and returns the first document in a selector query.
        /// </summary>
        /// <param name="javascriptWhere">The where.</param>
        /// <returns>
        /// A <see cref="Document"/> from the collection.
        /// </returns>
        public T FindOne(string javascriptWhere)
        {
            var spec = new Document { { "$where", new Code(javascriptWhere) } };
            using (var cursor = Find(spec).Limit(-1))
                return cursor.Documents.FirstOrDefault();
        }

        /// <summary>
        /// Finds and returns the first document in a query.
        /// </summary>
        /// <param name="selector">The spec.</param>
        /// <returns></returns>
        public T FindOne(Document selector)
        {
            using (var cursor = Find(selector).Limit(1))
                return cursor.Documents.FirstOrDefault();
        }

        /// <summary>
        /// Finds and returns the first document in a query.
        /// </summary>
        /// <typeparam name="TExample">The type of the example.</typeparam>
        /// <param name="spec">A <see cref="Document"/> representing the query.</param>
        /// <returns>
        /// A <see cref="Document"/> from the collection.
        /// </returns>
        public T FindOneByExample<TExample>(TExample selector)
        {
            return FindOne(ObjectToDocumentConverter.Convert(selector));
        }

        /// <summary>
        /// Finds all.
        /// </summary>
        /// <returns></returns>
        public ICursor<T> FindAll()
        {
            var spec = new Document();
            return Find(spec);
        }

        /// <summary>
        /// Finds the specified where.
        /// </summary>
        /// <param name="javascriptWhere">The where.</param>
        /// <returns></returns>
        public ICursor<T> Find(string javascriptWhere)
        {
            var spec = new Document { { "$where", new Code(javascriptWhere) } };
            return Find(spec);
        }

        /// <summary>
        /// Queries the collection using the query selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        public ICursor<T> Find(Document selector)
        {
            return new Cursor<T>(_configuration.SerializationFactory, _configuration.MappingStore, _connection, DatabaseName, Name).Spec(selector);
        }

        /// <summary>
        /// Finds the specified spec.
        /// </summary>
        /// <param name="selector">The spec.</param>
        /// <returns></returns>
        public ICursor<T> FindByExample<TExample>(TExample selector)
        {
            return Find(ObjectToDocumentConverter.Convert(selector));
        }

        /// <summary>
        /// Executes a query and atomically applies a modifier operation to the first document returning the original document
        /// by default.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="spec"><see cref="Document"/> to find the document.</param>
        /// <returns>A <see cref="Document"/></returns>
        public T FindAndModify(object document, object spec)
        {
            return FindAndModify(document, spec, false);
        }

        /// <summary>
        /// Executes a query and atomically applies a modifier operation to the first document returning the original document
        /// by default.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="spec"><see cref="Document"/> to find the document.</param>
        /// <param name="sort"><see cref="Document"/> containing the names of columns to sort on with the values being the</param>
        /// <returns>A <see cref="Document"/></returns>
        /// <see cref="IndexOrder"/>
        public T FindAndModify(object document, object spec, object sort)
        {
            return FindAndModify(document, spec, sort);
        }

        /// <summary>
        /// Executes a query and atomically applies a modifier operation to the first document returning the original document
        /// by default.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="spec"><see cref="Document"/> to find the document.</param>
        /// <param name="returnNew">if set to <c>true</c> [return new].</param>
        /// <returns>A <see cref="Document"/></returns>
        public T FindAndModify(object document, object spec, bool returnNew)
        {
            return FindAndModify(document, spec, new Document(), returnNew);
        }

        /// <summary>
        /// Executes a query and atomically applies a modifier operation to the first document returning the original document
        /// by default.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="spec"><see cref="Document"/> to find the document.</param>
        /// <param name="sort"><see cref="Document"/> containing the names of columns to sort on with the values being the
        /// <see cref="IndexOrder"/></param>
        /// <param name="returnNew">if set to <c>true</c> [return new].</param>
        /// <returns>A <see cref="Document"/></returns>
        public T FindAndModify(object document, object spec, object sort, bool returnNew)
        {
            try
            {
                var command = new Document
                {
                    {"findandmodify", Name},
                    {"query", spec},
                    {"update", EnsureUpdateDocument(document)},
                    {"sort", sort},
                    {"new", returnNew}
                };

                var response = _connection.SendCommand<FindAndModifyResult<T>>(_configuration.SerializationFactory,
                    DatabaseName,
                    typeof(T),
                    command);

                return response.Value;
            }
            catch (MongoCommandException)
            {
                // This is when there is no document to operate on
                return null;
            }
        }

        /// <summary>
        /// Entrypoint into executing a map/reduce query against the collection.
        /// </summary>
        /// <returns>A <see cref="MapReduce"/></returns>
        public MapReduce MapReduce()
        {
            return new MapReduce(Database, Name, typeof(T));
        }

        ///<summary>
        ///  Count all items in the collection.
        ///</summary>
        public long Count()
        {
            return Count(new Document());
        }

        /// <summary>
        /// Count all items in a collection that match the query spec.
        /// </summary>
        /// <param name="selector">The spec.</param>
        /// <returns></returns>
        /// <remarks>
        /// It will return 0 if the collection doesn't exist yet.
        /// </remarks>
        public long Count(Document selector)
        {
            return ((IMongoCollection)this).Count(selector);
        }

        public long CountByExample<TExample>(TExample selector)
        {
            return Count(ObjectToDocumentConverter.Convert(selector));
        }


        /// <summary>
        /// Inserts the specified document.
        /// </summary>
        /// <param name="document">The doc.</param>
        public void Insert(Document document)
        {
            InsertMany(new[] { document }, false);
        }

        /// <summary>
        /// Inserts the specified document.
        /// </summary>
        /// <param name="document">The document.</param>
        public void Insert(T document)
        {
            InsertMany(new[] { document }, false);
        }

        /// <summary>
        /// Inserts the by example.
        /// </summary>
        /// <typeparam name="TExample">The type of the example.</typeparam>
        /// <param name="example">The example.</param>
        public void InsertByExample<TExample>(TExample example)
        {
            InsertManyByExample<TExample>(new[] { example }, false);
        }

        /// <summary>
        /// Inserts the specified document.
        /// </summary>
        /// <param name="document">The doc.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void Insert(Document document, bool safemode)
        {
            InsertMany(new[] { document }, safemode);
        }

        /// <summary>
        /// Inserts the specified document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void Insert(T document, bool safemode)
        {
            InsertMany(new[] { document }, safemode);
        }

        /// <summary>
        /// Inserts the by example.
        /// </summary>
        /// <typeparam name="TExample">The type of the example.</typeparam>
        /// <param name="example">The example.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void InsertByExample<TExample>(TExample example, bool safemode)
        {
            InsertManyByExample(new[] { example }, safemode);
        }

        /// <summary>
        /// Inserts the specified documents.
        /// </summary>
        /// <param name="documents">The documents.</param>
        public void InsertMany(IEnumerable<Document> documents)
        {
            InsertMany(documents, false);
        }

        /// <summary>
        /// Inserts the specified documents.
        /// </summary>
        /// <param name="documents">The documents.</param>
        public void InsertMany(IEnumerable<T> documents)
        {
            InsertMany(documents, false);
        }

        /// <summary>
        /// Inserts the by example.
        /// </summary>
        /// <typeparam name="TExample">The type of the example.</typeparam>
        /// <param name="examples">The examples.</param>
        public void InsertManyByExample<TExample>(IEnumerable<TExample> examples)
        {
            InsertManyByExample<TExample>(examples, false);
        }

        /// <summary>
        /// Inserts the specified documents.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void InsertMany(IEnumerable<Document> documents, bool safemode)
        {
            ((IMongoCollection)this).InsertMany(documents, safemode);
        }

        /// <summary>
        /// Inserts the specified documents.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void InsertMany(IEnumerable<T> documents, bool safemode)
        {
            ((IMongoCollection)this).InsertMany(documents, safemode);
        }

        /// <summary>
        /// Inserts all.
        /// </summary>
        /// <typeparam name="TExample">The type of the example.</typeparam>
        /// <param name="examples">The documents.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void InsertManyByExample<TExample>(IEnumerable<TExample> examples, bool safemode)
        {
            InsertMany(examples.Select(x => ObjectToDocumentConverter.Convert(x)).ToArray(), safemode);
        }

        /// <summary>
        /// Deletes documents from the collection according to the spec.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        /// <remarks>
        /// An empty document will match all documents in the collection and effectively truncate it.
        /// </remarks>
        [Obsolete("Use Remove instead")]
        public void Delete(object selector, bool safemode)
        {
            Delete(selector);
            CheckError(safemode);
        }

        /// <summary>
        /// Remove documents from the collection according to the selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        /// <remarks>
        /// An empty document will match all documents in the collection and effectively truncate it.
        /// See the safemode description in the class description
        /// </remarks>
        public void Remove(object selector, bool safemode)
        {
            Remove(selector);
            CheckError(safemode);
        }

        /// <summary>
        /// Deletes documents from the collection according to the spec.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <remarks>
        /// An empty document will match all documents in the collection and effectively truncate it.
        /// </remarks>
        [Obsolete("Use Remove instead")]
        public void Delete(object selector)
        {
            var writerSettings = _configuration.SerializationFactory.GetBsonWriterSettings(typeof(T));

            try
            {
                _connection.SendMessage(new DeleteMessage(writerSettings)
                {
                    FullCollectionName = FullName,
                    Selector = selector
                }, DatabaseName);
            }
            catch (IOException exception)
            {
                throw new MongoConnectionException("Could not delete document, communication failure", _connection, exception);
            }
        }

        /// <summary>
        /// Remove documents from the collection according to the selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <remarks>
        /// An empty document will match all documents in the collection and effectively truncate it.
        /// </remarks>
        public void Remove(object selector)
        {
            var writerSettings = _configuration.SerializationFactory.GetBsonWriterSettings(typeof(T));

            try
            {
                _connection.SendMessage(new DeleteMessage(writerSettings)
                {
                    FullCollectionName = FullName,
                    Selector = selector
                }, DatabaseName);
            }
            catch (IOException exception)
            {
                throw new MongoConnectionException("Could not delete document, communication failure", _connection, exception);
            }
        }

        /// <summary>
        /// Updates the specified document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void Update(object document, object selector, bool safemode)
        {
            Update(document, selector, 0, safemode);
        }

        /// <summary>
        /// Updates a document with the data in doc as found by the selector.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        public void Update(object document, object selector)
        {
            Update(document, selector, 0);
        }

        /// <summary>
        /// Updates the specified document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void Update(object document, object selector, UpdateFlags flags, bool safemode)
        {
            Update(document, selector, flags);
            CheckError(safemode);
        }

        /// <summary>
        /// Updates a document with the data in doc as found by the selector.
        /// </summary>
        /// <param name="document">The <see cref="Document"/> to update with</param>
        /// <param name="selector">The query spec to find the document to update.</param>
        /// <param name="flags"><see cref="UpdateFlags"/></param>
        public void Update(object document, object selector, UpdateFlags flags)
        {
            var writerSettings = _configuration.SerializationFactory.GetBsonWriterSettings(typeof(T));

            try
            {
                _connection.SendMessage(new UpdateMessage(writerSettings)
                {
                    FullCollectionName = FullName,
                    Selector = selector,
                    Document = document,
                    Flags = (int)flags
                }, DatabaseName);
            }
            catch (IOException exception)
            {
                throw new MongoConnectionException("Could not update document, communication failure", _connection, exception);
            }
        }

        /// <summary>
        /// Runs a multiple update query against the database.  It will wrap any
        /// doc with $set if the passed in doc doesn't contain any '$' ops.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        public void UpdateAll(object document, object selector)
        {
            Update(EnsureUpdateDocument(document), selector, UpdateFlags.MultiUpdate);
        }

        /// <summary>
        /// Updates all.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        public void UpdateAll(object document, object selector, bool safemode)
        {
            if (safemode)
                Database.ResetError();
            UpdateAll(document, selector);
            CheckPreviousError(safemode);
        }

        public void Save(Document document)
        {
            Save(document, false);
        }

        public void Save(T document)
        {
            Save(document, false);
        }

        public void SaveByExample<TExample>(TExample example)
        {
            SaveByExample(example, false);
        }

        public void Save(Document document, bool safemode)
        {
            ((IMongoCollection)this).Save(document, safemode);
        }

        public void Save(T document, bool safemode)
        {
            ((IMongoCollection)this).Save(document, safemode);
        }

        public void SaveByExample<TExample>(TExample example, bool safemode)
        {
            ((IMongoCollection)this).Save(ObjectToDocumentConverter.Convert(example), safemode);
        }

        /// <summary>
        /// Checks the error.
        /// </summary>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        private void CheckError(bool safemode)
        {
            if (!safemode)
                return;

            var lastError = Database.GetLastError();

            if (ErrorTranslator.IsError(lastError))
                throw ErrorTranslator.Translate(lastError);
        }

        /// <summary>
        /// Checks the previous error.
        /// </summary>
        /// <param name="safemode">if set to <c>true</c> [safemode].</param>
        private void CheckPreviousError(bool safemode)
        {
            if (!safemode)
                return;

            var previousError = Database.GetPreviousError();

            if (ErrorTranslator.IsError(previousError))
                throw ErrorTranslator.Translate(previousError);
        }

        /// <summary>
        /// Ensures the update document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        private object EnsureUpdateDocument(object document)
        {
            var descriptor = _configuration.SerializationFactory.GetObjectDescriptor(typeof(T));

            var foundOp = descriptor.GetMongoPropertyNames(document)
                .Any(name => name.IndexOf('$') == 0);

            if (foundOp == false)
            {
                //wrap document in a $set.
                return new Document().Add("$set", document);
            }

            return document;
        }

        long IMongoCollection.Count()
        {
            return Count();
        }

        long IMongoCollection.Count(object selector)
        {
            try
            {
                var response = Database.SendCommand(typeof(T), new Document().Add("count", Name).Add("query", selector));
                return Convert.ToInt64((double)response["n"]);
            }
            catch (MongoCommandException)
            {
                //FIXME This is an exception condition when the namespace is missing. 
                //-1 might be better here but the console returns 0.
                return 0;
            }
        }

        void IMongoCollection.Insert(object document)
        {
            ((IMongoCollection)this).Insert(document, false);
        }

        void IMongoCollection.Insert(object document, bool safemode)
        {
            ((IMongoCollection)this).InsertMany(new[] { document }, safemode);
        }

        void IMongoCollection.InsertMany(IEnumerable documents)
        {
            ((IMongoCollection)this).InsertMany(documents, false);
        }

        void IMongoCollection.InsertMany(IEnumerable documents, bool safemode)
        {
            if (safemode)
                Database.ResetError();

            var rootType = typeof(T);
            var writerSettings = _configuration.SerializationFactory.GetBsonWriterSettings(rootType);

            var insertMessage = new InsertMessage(writerSettings)
            {
                FullCollectionName = FullName
            };

            var descriptor = _configuration.SerializationFactory.GetObjectDescriptor(rootType);
            var insertDocument = new ArrayList();

            foreach (var document in documents)
            {
                var id = descriptor.GetPropertyValue(document, "_id");

                if (id == null)
                    descriptor.SetPropertyValue(document, "_id", descriptor.GenerateId(document));

                insertDocument.Add(document);
            }

            insertMessage.Documents = insertDocument.ToArray();

            try
            {
                _connection.SendMessage(insertMessage, DatabaseName);
                CheckPreviousError(safemode);
            }
            catch (IOException exception)
            {
                throw new MongoConnectionException("Could not insert document, communication failure", _connection, exception);
            }
        }

        void IMongoCollection.Save(object document)
        {
            ((IMongoCollection)this).Save(document, false);
        }

        void IMongoCollection.Save(object document, bool safemode)
        {
            //Try to generate a selector using _id for an existing document.
            //otherwise just set the upsert flag to 1 to insert and send onward.

            var descriptor = _configuration.SerializationFactory.GetObjectDescriptor(typeof(T));

            var value = descriptor.GetPropertyValue(document, "_id");

            if (value == null)
            {
                //Likely a new document
                descriptor.SetPropertyValue(document, "_id", descriptor.GenerateId(value));

                ((IMongoCollection)this).Insert(document, safemode);
            }
            else
                Update(document, new Document("_id", value), UpdateFlags.Upsert);
        }
    }
}
