﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using achihapi.ViewModels;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

namespace achihapi.Controllers
{
    [Route("api/[controller]")]
    public class LearnCategoryController : Controller
    {
        // GET: api/learncategory
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<LearnCategoryViewModel> listVm = new List<LearnCategoryViewModel>();
            SqlConnection conn = new SqlConnection(Startup.DBConnectionString);
            String queryString = "";
            Boolean bError = false;
            String strErrMsg = "";

            try
            {
                queryString = @"SELECT [ID]
                              ,[PARID]
                              ,[NAME]
                              ,[COMMENT]
                              ,[SYSFLAG]
                              ,[CREATEDBY]
                              ,[CREATEDAT]
                              ,[UPDATEDBY]
                              ,[UPDATEDAT]
                          FROM [dbo].[t_learn_ctgy]";

                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand(queryString, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        LearnCategoryViewModel vm = new LearnCategoryViewModel();
                        onDB2VM(reader, vm);
                        listVm.Add(vm);
                    }
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                bError = true;
                strErrMsg = exp.Message;
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }

            if (bError)
                return StatusCode(500, strErrMsg);

            return new ObjectResult(listVm);
        }

        private void onDB2VM(SqlDataReader reader, LearnCategoryViewModel vm)
        {
            vm.ID = reader.GetInt32(0);
            if (!reader.IsDBNull(1))
                vm.ParID = reader.GetInt32(1);
            vm.Name = reader.GetString(2);
            if (!reader.IsDBNull(3))
                vm.Comment = reader.GetString(3);
            if (!reader.IsDBNull(4))
                vm.SysFlag = reader.GetBoolean(4);
            if (!reader.IsDBNull(5))
                vm.CreatedBy = reader.GetString(5);
            if (!reader.IsDBNull(6))
                vm.CreatedAt = reader.GetDateTime(6);
            if (!reader.IsDBNull(7))
                vm.UpdatedBy = reader.GetString(7);
            if (!reader.IsDBNull(8))
                vm.UpdatedAt = reader.GetDateTime(8);
        }

        // GET api/learncategory/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/learncategory
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody]LearnCategoryViewModel vm)
        {
            if (vm == null)
            {
                return BadRequest("No data is inputted");
            }

            if (vm.Name != null)
                vm.Name = vm.Name.Trim();
            if (String.IsNullOrEmpty(vm.Name))
            {
                return BadRequest("Name is a must!");
            }

            // Update the database
            SqlConnection conn = new SqlConnection(Startup.DBConnectionString);
            String queryString = "";
            Boolean bDuplicatedEntry = false;
            Int32 nDuplicatedID = -1;
            Int32 nNewID = -1;
            Boolean bError = false;
            String strErrMsg = "";
            var usr = User.FindFirst(c => c.Type == "sub");
            String usrName = String.Empty;
            if (usr != null)
                usrName = usr.Value;

            try
            {
                queryString = @"SELECT [ID]
                            FROM [dbo].[t_learn_ctgy] WHERE [Name] = N'" + vm.Name + "'";

                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand(queryString, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    bDuplicatedEntry = true;
                    while (reader.Read())
                    {
                        nDuplicatedID = reader.GetInt32(0);
                        break;
                    }
                }
                else
                {
                    reader.Dispose();
                    reader = null;

                    cmd.Dispose();
                    cmd = null;

                    // Now go ahead for the creating
                    queryString = @"INSERT INTO [dbo].[t_learn_ctgy]
                               ([PARID]
                               ,[NAME]
                               ,[COMMENT]
                               ,[SYSFLAG]
                               ,[CREATEDBY]
                               ,[CREATEDAT]
                               ,[UPDATEDBY]
                               ,[UPDATEDAT])
                         VALUES
                               (@PARID
                               ,@NAME
                               ,@COMMENT
                               ,@SYSFLAG
                               ,@CREATEDBY
                               ,@CREATEDAT
                               ,@UPDATEDBY
                               ,@UPDATEDAT) SELECT @Identity = SCOPE_IDENTITY();";

                    cmd = new SqlCommand(queryString, conn);
                    if (vm.ParID != null)
                    {
                        cmd.Parameters.AddWithValue("@PARID", vm.ParID.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PARID", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@NAME", vm.Name);
                    if (String.IsNullOrEmpty(vm.Comment))
                    {
                        cmd.Parameters.AddWithValue("@COMMENT", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@COMMENT", vm.Comment);
                    }
                    cmd.Parameters.AddWithValue("@SYSFLAG", vm.SysFlag);
                    cmd.Parameters.AddWithValue("@CREATEDBY", usrName);
                    cmd.Parameters.AddWithValue("@CREATEDAT", vm.CreatedAt);
                    cmd.Parameters.AddWithValue("@UPDATEDBY", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UPDATEDAT", DBNull.Value);
                    SqlParameter idparam = cmd.Parameters.AddWithValue("@Identity", SqlDbType.Int);
                    idparam.Direction = ParameterDirection.Output;

                    Int32 nRst = await cmd.ExecuteNonQueryAsync();
                    nNewID = (Int32)idparam.Value;
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                bError = true;
                strErrMsg = exp.Message;
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }

            if (bDuplicatedEntry)
            {
                return BadRequest("Object with name already exists: " + nDuplicatedID.ToString());
            }

            if (bError)
            {
                return StatusCode(500, strErrMsg);
            }

            vm.ID = nNewID;
            return new ObjectResult(vm);
        }

        // PUT api/learncategory/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/learncategory/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}