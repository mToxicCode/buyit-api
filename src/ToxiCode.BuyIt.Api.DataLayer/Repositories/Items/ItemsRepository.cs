using System.Text;
using Dapper;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.DeleteItems;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.GetItem;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.GetItems;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.GetItemsByAggregate;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.GetItemsById;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.InsertItems;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.MarkItemsForDeletion;
using ToxiCode.BuyIt.Api.DataLayer.Repositories.Items.Cmds.UpdateItems;
using ToxiCode.BuyIt.Api.Dtos;
using ToxiCode.BuyIt.Api.Dtos.Pagination;

namespace ToxiCode.BuyIt.Api.DataLayer.Repositories.Items;

public class ItemsRepository : IRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ItemsRepository(IDbConnectionFactory dbConnectionFactory)
        => _dbConnectionFactory = dbConnectionFactory;

    public async Task<GetItemByIdCmdResponse> GetItemById(GetItemByIdCmd cmd, CancellationToken cancellationToken)
    {
        const string getItemQuery = @"SELECT it.id, 
                                              it.name, 
                                              it.description, 
                                              it.created_at CreatedAt,
                                              it.owner_id OwnerId,
                                              it.price
                                           FROM items it WHERE id = @Id";

        const string getItemPicturesQuery = @"SELECT i.url, i.description
                                               FROM items_images im 
                                               INNER JOIN images i ON i.id = im.image_id
                                               WHERE im.item_id = @Id;";

        const string getReviewRatingQuery = @"SELECT AVG(r.rate) ""Value"", COUNT(1) ReviewCount 
                                              FROM reviews r
                                              WHERE item_id = @Id";

        const string getItemCategoryQuery = @"SELECT category_id 
                                              FROM items_category
                                              WHERE item_id = @Id";

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryFirstOrDefaultAsync<Item>(getItemQuery, cmd);

        var pictures = await connection.QueryAsync<Image>(getItemPicturesQuery, cmd);
        result.ImageUrls = pictures.Select(x => x.Url);

        result.Rating = await connection.QueryFirstAsync<Rating>(getReviewRatingQuery, cmd);
        result.CategoryIds = await connection.QueryAsync<long>(getItemCategoryQuery, cmd);
        return new GetItemByIdCmdResponse(result);
    }

    public async Task<GetItemsCmdResponse> GetItems(GetItemsCmd cmd, CancellationToken cancellationToken)
    {
        const string getItemsBaseQuery = @"SELECT it.id, 
                                              it.name, 
                                              it.description, 
                                              it.created_at CreatedAt,
                                              it.owner_id OwnerId,
                                              it.price
                                           FROM items it ";

        const string getPaginationInfoBaseQuery = @"SELECT COUNT(1) Amount
                                                     FROM items it ";

        const string getItemsPicturesQuery = @"SELECT i.url, i.description, i.id, i.file_name FileName
                                               FROM items_images im 
                                               INNER JOIN images i ON i.id = im.image_id
                                               WHERE im.item_id = @Id;";

        const string getReviewRatingQuery = @"SELECT AVG(r.rate) ""Value"", COUNT(1) ReviewCount 
                                              FROM reviews r
                                              WHERE item_id = @Id";

        const string getItemCategoryQuery = @"SELECT category_id 
                                              FROM items_category
                                              WHERE item_id = @Id";

        var filterQuery = @"";
        var paginationQuery = @"LIMIT @Limit OFFSET @Offset";

        var getItemsQuery = new StringBuilder(getItemsBaseQuery)
            .Append(filterQuery)
            .Append(paginationQuery);
        var getPaginationQuery = new StringBuilder(getPaginationInfoBaseQuery).Append(filterQuery);

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        var result = (await connection.QueryAsync<Item>(getItemsQuery.ToString(),
            new {Limit = cmd.Pagination.Count, cmd.Pagination.Offset})).ToArray();

        foreach (var item in result)
        {
            var pictures = await connection.QueryAsync<Image>(getItemsPicturesQuery, new {item.Id});
            item.ImageUrls = pictures.Select(x => x.Url).ToArray();

            item.Rating = await connection.QueryFirstAsync<Rating>(getReviewRatingQuery, new {item.Id});

            item.CategoryIds = await connection.QueryAsync<long>(getItemCategoryQuery, new {item.Id});
        }

        var pagination = await connection.QueryFirstAsync<PaginationResponse>(getPaginationQuery.ToString());

        return new(result, pagination);
    }

    public async Task<GetItemsByIdCmdResponse> GetItems(GetItemsByIdCmd cmd, CancellationToken cancellationToken)
    {
        const string getItemsQuery = @"SELECT it.id, 
                                              it.name, 
                                              it.description, 
                                              it.created_at CreatedAt,
                                              it.owner_id OwnerId,
                                              it.price
                                           FROM items it WHERE id = ANY(@Ids)";

        const string getItemPicturesQuery = @"SELECT i.url
                                               FROM items_images im 
                                               INNER JOIN images i ON i.id = im.image_id
                                               WHERE im.item_id = @Id;";

        const string getReviewRatingQuery = @"SELECT AVG(r.rate) ""Value"", COUNT(1) ReviewCount 
                                              FROM reviews r
                                              WHERE item_id = @Id";

        const string getItemCategoryQuery = @"SELECT category_id 
                                              FROM items_category
                                              WHERE item_id = @Id";

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        var result = (await connection.QueryAsync<Item>(getItemsQuery, cmd)).ToArray();

        foreach (var item in result)
        {
            item.ImageUrls = await connection.QueryAsync<string>(getItemPicturesQuery, new {item.Id});
            item.Rating = await connection.QueryFirstAsync<Rating>(getReviewRatingQuery, new {item.Id});
            item.CategoryIds = await connection.QueryAsync<long>(getItemCategoryQuery, new {item.Id});
        }

        return new GetItemsByIdCmdResponse()
        {
            Items = result
        };
    }

    public async Task<GetItemsByAggregateCmdResponse> GetItems(GetItemsByAggregateCmd cmd,
        CancellationToken cancellationToken)
    {
        const string getItemsQuery = @"SELECT it.id, 
                                              it.name, 
                                              it.description, 
                                              it.created_at CreatedAt,
                                              it.owner_id OwnerId,
                                              it.price
                                           FROM items it
                                           INNER JOIN items_category ic ON it.id = ic.item_id
                                           WHERE ic.category_id = @CategoryId;";

        const string getItemPicturesQuery = @"SELECT i.url
                                               FROM items_images im 
                                               INNER JOIN images i ON i.id = im.image_id
                                               WHERE item_id = @Id;";

        const string getReviewRatingQuery = @"SELECT AVG(r.rate) ""Value"", COUNT(1) ReviewCount 
                                              FROM reviews r
                                              WHERE item_id = @Id;";

        const string getItemCategoryQuery = @"SELECT category_id 
                                              FROM items_category
                                              WHERE item_id = @Id";

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        var result = (await connection.QueryAsync<Item>(getItemsQuery, cmd)).ToArray();

        foreach (var item in result)
        {
            item.ImageUrls = await connection.QueryAsync<string>(getItemPicturesQuery, new {item.Id});
            item.Rating = await connection.QueryFirstAsync<Rating>(getReviewRatingQuery, new {item.Id});
            item.CategoryIds = await connection.QueryAsync<long>(getItemCategoryQuery, new {item.Id});
        }

        return new GetItemsByAggregateCmdResponse
        {
            Items = result
        };
    }

    public async Task InsertItemsAsync(InsertItemsCmd cmd, CancellationToken cancellationToken)
    {
        const string insertItemsQuery = @"INSERT INTO items 
                                            (id, name, description, price, owner_id)
                                          VALUES (@Id, @Name, @Description, @Price, @Owner)";

        const string insertCatergoryForItems = @"INSERT INTO items_category
                                                    (item_id, category_id)
                                                    VALUES(@ItemId, @CategoryId)";

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(insertItemsQuery, cmd.Items.Select(x => new
        {
            x.Id,
            x.Name,
            x.Description,
            x.Price,
            Owner = x.OwnerId
        }));

        foreach (var itemToAdd in cmd.Items)
        {
            await connection.ExecuteAsync(insertCatergoryForItems, itemToAdd.CategoryIds.Select(y => new
            {
                ItemId = itemToAdd.Id,
                CategoryId = y
            }));
        }
    }

    public async Task UpdateItemsAsync(UpdateItemsCmd cmd, CancellationToken cancellationToken)
    {
        const string updateItemsQuery = @"UPDATE items SET 
                                            name = @Name, 
                                            description = @Description, 
                                            price = @Price, 
                                            owner_id = @Owner
                                          WHERE id = @Id";

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(updateItemsQuery, cmd.Items.Select(x => new
        {
            x.Id,
            x.Name,
            x.Description,
            x.Price,
            Owner = x.OwnerId
        }));
    }

    public async Task DeleteItemsAsync(DeleteItemsCmd cmd, CancellationToken cancellationToken)
    {
        const string deleteItemsQuery = @"DELETE FROM items WHERE id = @Id;";

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(deleteItemsQuery, cmd.ItemsIds.Select(x => new
        {
            Id = x
        }));
    }

    public async Task MarkItemsForDeletionAsync(MarkItemsForDeletionCmd cmd, CancellationToken cancellationToken)
    {
        const string markItemsForDeletion = @"UPDATE items SET deleted_at = current_timestamp WHERE id = @Id";

        await using var connection = _dbConnectionFactory.CreateDatabase().Connection;
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(markItemsForDeletion, cmd.ItemsIds.Select(x => new
        {
            Id = x
        }));
    }
}