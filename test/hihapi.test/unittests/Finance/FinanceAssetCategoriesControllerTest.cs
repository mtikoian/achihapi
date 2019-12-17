using System;
using Xunit;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using hihapi.Models;
using hihapi.Controllers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNet.OData.Results;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace hihapi.test.UnitTests
{
    [Collection("HIHAPI_UnitTests#1")]
    public class FinanceAssetCategoriesControllerTest
    {
        SqliteDatabaseFixture fixture = null;

        public FinanceAssetCategoriesControllerTest(SqliteDatabaseFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task TestCase1_Home1()
        {
            var context = this.fixture.GetCurrentDataContext();
            var ctgyCount = DataSetupUtility.FinanceAssetCategories.Count();

            FinanceAssetCategoriesController control = new FinanceAssetCategoriesController(context);
            var user = DataSetupUtility.GetClaimForUser(DataSetupUtility.UserA);
            control.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // 1. Read all categories
            var items = control.Get();
            var itemcnt = items.Count();
            Assert.Equal(ctgyCount, itemcnt);

            // 2. Insert new category
            var ctgy = new FinanceAssetCategory()
            {
                HomeID = DataSetupUtility.Home1ID,
                Name = "Test 1",
                Desp = "Test 1"
            };
            var rst1 = await control.Post(ctgy);
            Assert.NotNull(rst1);
            var rst2 = Assert.IsType<CreatedODataResult<FinanceAssetCategory>>(rst1);
            Assert.Equal(ctgy.Name, rst2.Entity.Name);
            var firstctg = rst2.Entity.ID;
            Assert.True(firstctg > 0);
            Assert.Equal(ctgy.Desp, rst2.Entity.Desp);

            // 3. Read all categories, again
            items = control.Get(DataSetupUtility.Home1ID);
            itemcnt = items.Count();
            Assert.Equal(ctgyCount + 1, itemcnt);

            // 4. Change it
            ctgy.Name = "Test 2";
            rst1 = await control.Put(firstctg, ctgy);
            var rst3 = Assert.IsType<UpdatedODataResult<FinanceAssetCategory>>(rst1);
            Assert.Equal(ctgy.Name, rst3.Entity.Name);

            // 5. Delete it
            var rst4 = await control.Delete(firstctg);
            Assert.NotNull(rst4);
            var rst6 = Assert.IsType<StatusCodeResult>(rst4);
            Assert.Equal(204, rst6.StatusCode);

            // 6. Read all categories again
            items = control.Get(DataSetupUtility.Home1ID);
            itemcnt = items.Count();
            Assert.Equal(ctgyCount, itemcnt);

            await context.DisposeAsync();
        }
    }
}
