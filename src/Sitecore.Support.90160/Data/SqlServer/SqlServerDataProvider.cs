﻿namespace Sitecore.Support.Data.SqlServer
{
    using Sitecore.Data;
    using Sitecore.Data.DataProviders.Sql;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Configuration;

    public class SqlServerDataProvider : Sitecore.Data.SqlServer.SqlServerDataProvider
    {
        public SqlServerDataProvider(string connectionString)
          : base(connectionString)
        {
        }

        protected override void Descendants_ItemDeleted(ID itemId)
        {
            if (this.SkipDescendantsUpdate)
            {
                return;
            }
            try
            {
                Factory.GetRetryer().ExecuteNoResult(delegate
                {
                    using (DataProviderTransaction transaction = base.Api.CreateTransaction())
                    {
                        base.Api.Execute("DELETE FROM {0}Descendants{1} WHERE {0}Descendant{1} IN (SELECT {0}Descendant{1} FROM {0}Descendants{1} WHERE {0}Ancestor{1} = {2}itemId{3})", new object[] { "itemId", itemId });
                        base.Api.Execute("DELETE FROM {0}Descendants{1} WHERE {0}Descendant{1} = {2}itemId{3}", new object[] { "itemId", itemId });
                        transaction.Complete();
                    }
                });
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString(), this);
            }
            this.RebuildThread = null;
        }



        protected override void Descendants_ItemCreated(ID parentId, ID itemId)
        {
            if (this.SkipDescendantsUpdate)
            {
                return;
            }
            if (parentId.IsNull)
            {
                return;
            }
            try
            {
                Factory.GetRetryer().ExecuteNoResult(delegate
                {
                    using (DataProviderTransaction dataProviderTransaction = base.Api.CreateTransaction())
                    {
                        base.Api.Execute("INSERT INTO {0}Descendants{1} ({0}ID{1}, {0}Ancestor{1}, {0}Descendant{1})\r\n                         VALUES (newid(), {2}parentId{3}, {2}childId{3})", new object[] { "parentId", parentId, "childId", itemId });
                        base.Api.Execute("INSERT INTO {0}Descendants{1} ({0}ID{1}, {0}Ancestor{1}, {0}Descendant{1})\r\n                         SELECT newid(), {0}Ancestor{1}, {2}itemId{3}\r\n                         FROM {0}Descendants{1} \r\n                         WHERE {0}Descendant{1} = {2}parentId{3}", new object[] { "itemId", itemId, "parentId", parentId });
                        dataProviderTransaction.Complete();
                    }
                });
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString(), this);
            }
            this.RebuildThread = null;
            if ((base.Database.GetItem(itemId) != null) && (base.Database.GetItem(itemId).Children.Count > 0))
            {
                foreach (Item item in base.Database.GetItem(itemId).Children)
                {
                    this.Descendants_ItemCreated(itemId, item.ID);
                }
            }
        }
    }
}
