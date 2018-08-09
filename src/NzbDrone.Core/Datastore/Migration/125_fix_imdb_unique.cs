﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using System.Data;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(125)]
    public class fix_imdb_unique : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(DeleteUniqueIndex);
        }

        private void DeleteUniqueIndex(IDbConnection conn, IDbTransaction tran)
        {
            using (IDbCommand getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"DROP INDEX 'IX_Movies_ImdbId'";

                getSeriesCmd.ExecuteNonQuery();
            }
        }

    }
}
