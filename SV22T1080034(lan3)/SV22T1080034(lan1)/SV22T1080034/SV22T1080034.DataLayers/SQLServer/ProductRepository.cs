using Dapper;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.DomainModels.Common;

namespace SV22T1080034.DataLayers.SQLServer
{
    public class ProductRepository : BaseRepository, IProductRepository
    {
        public ProductRepository(string connectionString) : base(connectionString) { }

        public async Task<int> AddAsync(Product data)
        {
            using var connection = GetConnection();

            string sql = @"INSERT INTO Products(ProductName, Unit, Price, Photo, CategoryID, SupplierID, ProductDescription, IsSelling)
                   VALUES(@ProductName, @Unit, @Price, @Photo, @CategoryID, @SupplierID, @ProductDescription, @IsSelling);
                   SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = GetConnection();
            string sql = "DELETE FROM Products WHERE ProductID=@productID";
            return await connection.ExecuteAsync(sql, new { productID }) > 0;
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = GetConnection();

            string sql = @"
                            SELECT P.*, 
                                   C.CategoryName,
                                   S.SupplierName
                            FROM Products P
                            LEFT JOIN Categories C ON P.CategoryID = C.CategoryID
                            LEFT JOIN Suppliers S ON P.SupplierID = S.SupplierID
                            WHERE P.ProductID = @productID";
            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { productID });
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Products
                   SET ProductName=@ProductName,
                       Unit=@Unit,
                       Price=@Price,
                       Photo=@Photo,
                       CategoryID=@CategoryID,
                       SupplierID=@SupplierID,
                       ProductDescription=@ProductDescription,
                       IsSelling=@IsSelling
                   WHERE ProductID=@ProductID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = GetConnection();

            string sql = "SELECT COUNT(*) FROM OrderDetails WHERE ProductID=@productID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { productID });

            return count > 0;
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = GetConnection();

            string where = "WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
                where += " AND P.ProductName LIKE @search ";

            if (input.CategoryID > 0)
                where += " AND P.CategoryID = @CategoryID "; 

            if (input.SupplierID > 0)
                where += " AND P.SupplierID = @SupplierID "; 

            if (input.MinPrice > 0)
                where += " AND P.Price >= @MinPrice "; 

            if (input.MaxPrice > 0)
                where += " AND P.Price <= @MaxPrice "; 

            string sqlCount = $@"
                                SELECT COUNT(*)
                                FROM Products P
                                {where}";

            string sqlData = $@"
                                SELECT P.ProductID, P.ProductName, P.Unit, P.Price, P.Photo,
                                       P.CategoryID, P.SupplierID, P.IsSelling,
                                       C.CategoryName, S.SupplierName
                                FROM Products P
                                LEFT JOIN Categories C ON P.CategoryID = C.CategoryID
                                LEFT JOIN Suppliers S ON P.SupplierID = S.SupplierID
                                {where}
                                ORDER BY P.ProductName
                                OFFSET @offset ROWS
                                FETCH NEXT @pagesize ROWS ONLY";

            var param = new
            {
                search = $"%{input.SearchValue}%",
                input.CategoryID,
                input.SupplierID,
                input.MinPrice,
                input.MaxPrice,
                offset = (input.Page - 1) * input.PageSize,
                pagesize = input.PageSize
            };

            int count = await connection.ExecuteScalarAsync<int>(sqlCount, param);

            var data = await connection.QueryAsync<Product>(sqlData, param);

            return new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        // ATTRIBUTE

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = GetConnection();
            string sql = "SELECT * FROM ProductAttributes WHERE ProductID=@productID";
            var data = await connection.QueryAsync<ProductAttribute>(sql, new { productID });
            return data.ToList();
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = GetConnection();
            string sql = "SELECT * FROM ProductAttributes WHERE AttributeID=@attributeID";
            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { attributeID });
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = GetConnection();

            string sql = @"INSERT INTO ProductAttributes(ProductID,AttributeName,AttributeValue,DisplayOrder)
                           VALUES(@ProductID,@AttributeName,@AttributeValue,@DisplayOrder);
                           SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE ProductAttributes
                           SET AttributeName=@AttributeName,
                               AttributeValue=@AttributeValue,
                               DisplayOrder=@DisplayOrder
                           WHERE AttributeID=@AttributeID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = GetConnection();
            string sql = "DELETE FROM ProductAttributes WHERE AttributeID=@attributeID";
            return await connection.ExecuteAsync(sql, new { attributeID }) > 0;
        }

        // PHOTO

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = GetConnection();
            string sql = "SELECT * FROM ProductPhotos WHERE ProductID=@productID";
            var data = await connection.QueryAsync<ProductPhoto>(sql, new { productID });
            return data.ToList();
        }

        public async Task<ProductPhoto?> GetPhotosAsync(long photoID)
        {
            using var connection = GetConnection();
            string sql = "SELECT * FROM ProductPhotos WHERE PhotoID=@photoID";
            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { photoID });
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = GetConnection();

            string sql = @"INSERT INTO ProductPhotos(ProductID,Photo,Description,DisplayOrder,IsHidden)
                           VALUES(@ProductID,@Photo,@Description,@DisplayOrder,@IsHidden);
                           SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE ProductPhotos
                           SET Photo=@Photo,
                               Description=@Description,
                               DisplayOrder=@DisplayOrder,
                               IsHidden=@IsHidden
                           WHERE PhotoID=@PhotoID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = GetConnection();
            string sql = "DELETE FROM ProductPhotos WHERE PhotoID=@photoID";
            return await connection.ExecuteAsync(sql, new { photoID }) > 0;
        }
    }
}