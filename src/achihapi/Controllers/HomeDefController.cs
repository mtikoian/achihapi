﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using achihapi.ViewModels;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace achihapi.Controllers
{
    [Route("api/[controller]")]
    public class HomeDefController : Controller
    {
        /**
         * Create
         *      When a user create a Home defintion, it will be the host automatically.
         *          Insert an entry in t_homedef, Insert an entry in t_homemem;
         *          
         * Assign an user
         *      The host can add an user into home definition; Todo: add t_homemessage?
         *          Insert an entry in t_homemem;
         * 
         * Remove an user
         *      The host can remove an user from the home definition;
         *          Delete an entry from t_homemem;
         * 
         * Handover the host
         *      The host can name another user in the home to be the host;
         *          Change the t_homedef directly;
         * 
         * Login integration
         *      1) When an user is login but no home assigned, prompt the user to create the home definition;
         *      2) When an user is login, fetch all home definitions relevant (via t_homemember), and let the user choose one;
         * 
         */
         
        // GET: api/homedef
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get([FromQuery]Int32 top = 100, Int32 skip = 0)
        {
            BaseListViewModel<HomeDefViewModel> listVm = new BaseListViewModel<HomeDefViewModel>();
            SqlConnection conn = new SqlConnection(Startup.DBConnectionString);
            String queryString = "";
            Boolean bError = false;
            String strErrMsg = "";

            try
            {
                String scopeFilter = String.Empty;

                String usrName = "";
                try
                {
                    var usrObj = HIHAPIUtility.GetUserClaim(this);
                    usrName = usrObj.Value;

                    // Disabled scope check just make it work, 2017.10.1
                    scopeFilter = usrName;

                    //var scopeObj = HIHAPIUtility.GetScopeClaim(this, HIHAPIConstants.HomeDefScope);

                    //scopeFilter = HIHAPIUtility.GetScopeSQLFilter(scopeObj.Value, usrName);
                    //if (String.IsNullOrEmpty(scopeFilter))
                    //    scopeFilter = usrName;
                }
                catch
                {
                    return BadRequest("Not valid HTTP HEAD: User and Scope Failed!");
                }

                if (String.IsNullOrEmpty(scopeFilter))
                {
                    return BadRequest();
                }

                queryString = this.getQueryString(true, top, skip, null, scopeFilter);

                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand(queryString, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                Int32 nRstBatch = 0;
                while (reader.HasRows)
                {
                    if (nRstBatch == 0)
                    {
                        while (reader.Read())
                        {
                            listVm.TotalCount = reader.GetInt32(0);
                            break;
                        }
                    }
                    else
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                HomeDefViewModel vm = new HomeDefViewModel();
                                SqlUtility.HomeDef_DB2VM(reader, vm);
                                listVm.Add(vm);
                            }
                        }
                    }
                    ++nRstBatch;

                    reader.NextResult();
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
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            if (bError)
            {
                return StatusCode(500, strErrMsg);
            }

            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            
            return new JsonResult(listVm, setting);
        }

        // GET api/homedef/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(int id)
        {
            HomeDefViewModel vm = new HomeDefViewModel();
            SqlConnection conn = new SqlConnection(Startup.DBConnectionString);
            String queryString = "";
            Boolean bExist = false;
            Boolean bError = false;
            String strErrMsg = "";

            try
            {
                String scopeFilter = String.Empty;
                String usrName = "";
                try
                {
                    var usrObj = HIHAPIUtility.GetUserClaim(this);
                    usrName = usrObj.Value;

                    // Disabled scope check just make it work, 2017.10.1
                    scopeFilter = usrName;
                    //var scopeObj = HIHAPIUtility.GetScopeClaim(this, HIHAPIConstants.HomeDefScope);

                    //scopeFilter = HIHAPIUtility.GetScopeSQLFilter(scopeObj.Value, usrName);
                }
                catch
                {
                    return BadRequest("Not valid HTTP HEAD: User and Scope Failed!");
                }

                queryString = this.getQueryString(false, null, null, id, scopeFilter);

                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand(queryString, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    // Header part
                    bExist = true;
                    while (reader.Read())
                    {
                        SqlUtility.HomeDef_DB2VM(reader, vm);

                        // It should return one entry only!
                        // Nevertheless, ensure the code only execute once in API layer to keep toilence of dirty DB data;

                        break;
                    }

                    reader.NextResult();

                    while(reader.Read())
                    {
                        HomeMemViewModel vmMem = new HomeMemViewModel();
                        SqlUtility.HomeMem_DB2VM(reader, vmMem);
                        vm.Members.Add(vmMem);
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
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            // In case not found, return a 404
            if (!bExist)
                return NotFound();
            else if (bError)
                return StatusCode(500, strErrMsg);

            // Only return the meaningful object
            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            
            return new JsonResult(vm, setting);
        }

        // POST api/homedef
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody]HomeDefViewModel vm)
        {
            String usrId = "";
            try
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrId = usrObj.Value;

                // Disabled scope check just make it work, 2017.10.1

                //var scopeObj = HIHAPIUtility.GetScopeClaim(this, HIHAPIConstants.HomeDefScope);

                //if (String.CompareOrdinal(scopeObj.Value, HIHAPIConstants.OnlyOwnerAndDispaly) == 0)
                //{
                //    return StatusCode(401, "Current user has no authority to create home!");
                //}
                //else if (String.CompareOrdinal(scopeObj.Value, HIHAPIConstants.OnlyOwnerFullControl) == 0)
                //{
                //    if (String.CompareOrdinal(vm.Host, usrId) != 0)
                //    {
                //        return StatusCode(401, "Current user can only create home with owner.");
                //    }
                //}
            }
            catch
            {
                return BadRequest("Not valid HTTP HEAD: User and Scope Failed!");
            }

            if (vm.Name != null)
                vm.Name = vm.Name.Trim();
            if (String.IsNullOrEmpty(vm.Name))
            {
                return BadRequest("Name is a must!");
            }

            // Update the database
            SqlConnection conn = null;
            SqlTransaction tran = null;

            String queryString = "";
            Int32 nNewID = -1;
            Boolean bError = false;
            String strErrMsg = "";

            try
            {
                conn = new SqlConnection(Startup.DBConnectionString);
                conn.Open();
                tran = conn.BeginTransaction();
                queryString = SqlUtility.getHomeDefInsertString();
                SqlCommand cmd = new SqlCommand(queryString, conn)
                {
                    Transaction = tran
                };

                // Home def.
                SqlUtility.bindHomeDefInsertParameter(cmd, vm, usrId);
                SqlParameter idparam = cmd.Parameters.AddWithValue("@Identity", SqlDbType.Int);
                idparam.Direction = ParameterDirection.Output;

                Int32 nRst = await cmd.ExecuteNonQueryAsync();
                nNewID = (Int32)idparam.Value;

                // Home members
                cmd.Dispose();
                cmd = null;
                queryString = SqlUtility.getHomeMemInsertString();
                cmd = new SqlCommand(queryString, conn)
                {
                    Transaction = tran
                };
                HomeMemViewModel vmMem = new HomeMemViewModel
                {
                    HomeID = nNewID,
                    CreatedBy = usrId,
                    CreatedAt = DateTime.Now,
                    User = usrId,
                    DisplayAs = vm.CreatorDisplayAs,
                    Relation = (Int16)(HIHHomeMemberRelationship.Self)
                };
                SqlUtility.bindHomeMemInsertParameter(cmd, vmMem, usrId);
                await cmd.ExecuteNonQueryAsync();

                tran.Commit();
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                bError = true;
                strErrMsg = exp.Message;

                if (tran != null)
                    tran.Rollback();
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            if (bError)
                return StatusCode(500, strErrMsg);

            vm.ID = nNewID;
            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            
            return new JsonResult(vm, setting);
        }

        // PUT api/homedef/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(int id, [FromBody]HomeDefViewModel vm)
        {
            if (vm.ID != id)
            {
                return BadRequest("ID is not match!");
            }

            String usrName = "";
            try
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrName = usrObj.Value;

                // Disabled scope check just make it work, 2017.10.1

                //var scopeObj = HIHAPIUtility.GetScopeClaim(this, HIHAPIConstants.HomeDefScope);

                //if (String.CompareOrdinal(scopeObj.Value, HIHAPIConstants.OnlyOwnerAndDispaly) == 0)
                //{
                //    return StatusCode(401, "Current user has no authority to create home!");
                //}
                //else if (String.CompareOrdinal(scopeObj.Value, HIHAPIConstants.OnlyOwnerFullControl) == 0)
                //{
                //    if (String.CompareOrdinal(vm.Host, usrName) != 0)
                //    {
                //        return StatusCode(401, "Current user can only create home with owner.");
                //    }
                //}
            }
            catch
            {
                return BadRequest("Not valid HTTP HEAD: User and Scope Failed!");
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
            Int32 nNewID = -1;
            Boolean bError = false;
            String strErrMsg = "";

            try
            {
                queryString = SqlUtility.getHomeDefUpdateString();
                SqlCommand cmd = new SqlCommand(queryString, conn);

                SqlUtility.bindHomeDefUpdateParameter(cmd, vm, usrName);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                bError = true;
                strErrMsg = exp.Message;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            if (bError)
                return StatusCode(500, strErrMsg);

            vm.ID = nNewID;
            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            
            return new JsonResult(vm, setting);
        }

        // DELETE api/homedef/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            if (id == 0)
            {
                return BadRequest("ID is not match!");
            }

            String usrName = "";
            try
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrName = usrObj.Value;
                var scopeObj = HIHAPIUtility.GetScopeClaim(this, HIHAPIConstants.HomeDefScope);

                if (String.CompareOrdinal(scopeObj.Value, HIHAPIConstants.OnlyOwnerAndDispaly) == 0)
                {
                    return StatusCode(401, "Current user has no authority to create home!");
                }
                else if (String.CompareOrdinal(scopeObj.Value, HIHAPIConstants.OnlyOwnerFullControl) == 0)
                {
                    // To do!!
                    //if (String.CompareOrdinal(vm.Host, usrName) != 0)
                    //{
                    //    return StatusCode(401, "Current user can only create home with owner.");
                    //}
                }
            }
            catch
            {
                return BadRequest("Not valid HTTP HEAD: User and Scope Failed!");
            }

            // Update the database
            SqlConnection conn = new SqlConnection(Startup.DBConnectionString);
            String queryString = "";
            Boolean bError = false;
            String strErrMsg = "";

            try
            {
                queryString = SqlUtility.getHomeDefDeleteString();
                SqlCommand cmd = new SqlCommand(queryString, conn);
                cmd.Parameters.AddWithValue("@ID", id);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                bError = true;
                strErrMsg = exp.Message;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            if (bError)
                return StatusCode(500, strErrMsg);

            return Ok();
        }

        #region Implementation methods
        private string getQueryString(Boolean bListMode, Int32? nTop, Int32? nSkip, Int32? nSearchID, String strUserID)
        {

            String strSQL = "";
            if (bListMode)
            {
                strSQL += @"SELECT count(*) FROM [dbo].[v_homemember] ";
                if (!String.IsNullOrEmpty(strUserID))
                {
                    strSQL += " WHERE [USER] = N'" + strUserID + "'";
                }
                strSQL += " ;";
            }

            strSQL += SqlUtility.getHomeDefQueryString(strUserID);

            if (bListMode && nTop.HasValue && nSkip.HasValue)
            {
                strSQL += @" ORDER BY (SELECT NULL)
                        OFFSET " + nSkip.Value.ToString() + " ROWS FETCH NEXT " + nTop.Value.ToString() + " ROWS ONLY;";
            }
            else if (!bListMode && nSearchID.HasValue)
            {
                if (!String.IsNullOrEmpty(strUserID))
                {
                    strSQL += @" AND [v_homemember].[ID] = " + nSearchID.Value.ToString() + "; ";
                }
                else
                {
                    strSQL += @" WHERE [v_homemember].[ID] = " + nSearchID.Value.ToString() +"; ";
                }

                // Add home member part
                strSQL += SqlUtility.getHomeMemQueryString(nSearchID);
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("HomeDefController, SQL generated: " + strSQL);
#endif

            return strSQL;
        }
        #endregion
    }
}
