﻿namespace BuyIt.Api.DataLayer
{
    public interface IDbConnectionFactory
    {
        DatabaseWrapper CreateDatabase(CancellationToken? cancellationToken = default);
    }
}