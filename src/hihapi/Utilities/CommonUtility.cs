using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using hihapi.Models;

namespace hihapi.Utilities
{
    internal class HIHAPIConstants
    {
        public const String OnlyOwnerAndDispaly = "OnlyOwnerAndDisplay";
        public const String OnlyOwnerFullControl = "OnlyOwnerFullControl";
        public const String OnlyOwner = "OnlyOwner";
        public const String Display = "Display";
        public const String All = "All";

        internal const String HomeDefScope = "HomeDefScope";
        internal const String FinanceAccountScope = "FinanceAccountScope";
        internal const String FinanceDocumentScope = "FinanceDocumentScope";
        internal const String LearnHistoryScope = "LearnHistoryScope";
        internal const String LearnObjectScope = "LearnObjectScope";

        internal const String DateFormatPattern = "yyyy-MM-dd";
    }

    internal static class HIHAPIUtility
    {
        internal static String GetUserID(Microsoft.AspNetCore.Mvc.ControllerBase ctrl)
        {
            return ctrl.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

/*
        internal static void CheckHIDAssignment(hihDataContext context, Int32 hid, String usrName)
        {
            var ncnt = context.HomeMembers.Where(p => p.HomeID == hid && p.User == usrName).Count();
            if (ncnt <= 0)
                throw new Exception("No Home Definition found");
        }
        */
        // internal static System.Security.Claims.Claim GetUserClaim(Microsoft.AspNetCore.Mvc.ControllerBase ctrl)
        // {
        //     var usrObj = ctrl.User.FindFirst(c => c.Type == "sub");
        //     if (usrObj == null)
        //         throw new Exception();

        //     return usrObj;
        // }

        // internal static System.Security.Claims.Claim GetScopeClaim(Microsoft.AspNetCore.Mvc.ControllerBase ctrl, String strScope)
        // {
        //     var scopeObj = ctrl.User.FindFirst(c => c.Type == strScope);
        //     if (scopeObj == null)
        //         throw new Exception();

        //     return scopeObj;
        // }

        // internal static String GetScopeSQLFilter(String scopeStr, String usrStr)
        // {
        //     if (String.CompareOrdinal(scopeStr, HIHAPIConstants.All) == 0)
        //     {
        //         scopeStr = String.Empty;
        //     }
        //     else if (String.CompareOrdinal(scopeStr, HIHAPIConstants.OnlyOwnerAndDispaly) == 0)
        //     {
        //         scopeStr = usrStr;
        //     }
        //     else if (String.CompareOrdinal(scopeStr, HIHAPIConstants.OnlyOwnerFullControl) == 0)
        //     {
        //         scopeStr = usrStr;
        //     }

        //     return scopeStr;
        // }

//         internal static void CheckHIDAssignment(SqlConnection conn, Int32 hid, String usrName)
//         {
//             if (hid == 0 || conn == null || String.IsNullOrEmpty(usrName))
//                 throw new Exception("Inputted parameter invalid");

//             String strHIDCheck = @"SELECT TOP (1) [HID] FROM [dbo].[t_homemem] WHERE [HID]= @hid AND [USER] = @user";
//             SqlCommand cmdHIDCheck = null;
//             SqlDataReader readHIDCheck = null;

//             try
//             {
//                 cmdHIDCheck = new SqlCommand(strHIDCheck, conn);                
//                 cmdHIDCheck.Parameters.AddWithValue("@hid", hid);
//                 cmdHIDCheck.Parameters.AddWithValue("@user", usrName);
//                 readHIDCheck = cmdHIDCheck.ExecuteReader();
//                 if (!readHIDCheck.HasRows)
//                     throw new Exception("No Home Definition found");
//             }
//             catch (Exception exp)
//             {
// #if DEBUG
//                  System.Diagnostics.Debug.WriteLine(exp.Message);
// #endif
//                 // Re-throw the exception
//                 throw exp;
//             }
//             finally
//             {
//                 if (readHIDCheck != null)
//                 {
//                     readHIDCheck.Dispose();
//                     readHIDCheck = null;
//                 }

//                 if (cmdHIDCheck != null)
//                 {
//                     cmdHIDCheck.Dispose();
//                     cmdHIDCheck = null;
//                 }
//             }
//         }
    }

    public class CommonUtility
    {
        public static List<RepeatedDates> WorkoutRepeatedDates(RepeatDatesCalculationInput datInput)
        {
            List<RepeatedDates> listResults = new List<RepeatedDates>();

            // Input checks
            if (datInput == null)
                throw new Exception("Input the data!");
            var dtEnd = new DateTime(datInput.EndDate.Year, datInput.EndDate.Month, datInput.EndDate.Day);
            var dtStart = new DateTime(datInput.StartDate.Year, datInput.StartDate.Month, datInput.StartDate.Day);
            if (dtEnd < dtStart)
                throw new Exception("Invalid data range");

            switch (datInput.RepeatType)
            {
                case RepeatFrequency.Day:
                    {
                        var tspans = dtEnd - dtStart;
                        var tdays = (Int32)tspans.Days;

                        for (int i = 0; i <= tdays; i++)
                        {
                            listResults.Add(new RepeatedDates
                            {
                                StartDate = dtStart.AddDays(i),
                            });
                        }

                        for (int i = 0; i < listResults.Count; i++)
                        {
                            listResults[i].EndDate = listResults[i].StartDate;
                        }
                    }
                    break;

                case RepeatFrequency.Fortnight:
                    {
                        var tspans = dtEnd - dtStart;
                        var tdays = (Int32)tspans.Days;

                        var tfortnights = tdays / 14;

                        for (int i = 0; i <= tfortnights; i++)
                        {
                            listResults.Add(new RepeatedDates
                            {
                                StartDate = datInput.StartDate.AddDays(i * 14),
                            });
                        }

                        for (int i = 0; i < listResults.Count; i++)
                        {
                            if (i == listResults.Count - 1)
                            {
                                listResults[i].EndDate = listResults[i].StartDate.AddDays(13);
                            }
                            else
                            {
                                listResults[i].EndDate = listResults[i + 1].StartDate.AddDays(-1);
                            }
                        }
                    }
                    break;

                case RepeatFrequency.HalfYear:
                    {
                        var nmonths = (datInput.EndDate.Year - datInput.StartDate.Year) * 12 + (datInput.EndDate.Month - datInput.StartDate.Month);
                        var nhalfyear = nmonths / 6;

                        for (int i = 0; i <= nhalfyear; i++)
                        {
                            listResults.Add(new RepeatedDates
                            {
                                StartDate = datInput.StartDate.AddMonths(i * 6),
                            });
                        }

                        for (int i = 0; i < listResults.Count; i++)
                        {
                            if (i == listResults.Count - 1)
                            {
                                listResults[i].EndDate = listResults[i].StartDate.AddMonths(6);
                            }
                            else
                            {
                                listResults[i].EndDate = listResults[i + 1].StartDate.AddDays(-1);
                            }
                        }
                    }
                    break;

                case RepeatFrequency.Month:
                    {
                        var nmonths = (datInput.EndDate.Year - datInput.StartDate.Year) * 12 + (datInput.EndDate.Month - datInput.StartDate.Month);

                        for (int i = 0; i <= nmonths; i++)
                        {
                            listResults.Add(new RepeatedDates
                            {
                                StartDate = datInput.StartDate.AddMonths(i),
                            });
                        }

                        for (int i = 0; i < listResults.Count; i++)
                        {
                            if (i == listResults.Count - 1)
                            {
                                listResults[i].EndDate = listResults[i].StartDate.AddMonths(1).AddDays(-1);
                            }
                            else
                            {
                                listResults[i].EndDate = listResults[i + 1].StartDate.AddDays(-1);
                            }
                        }
                    }
                    break;

                case RepeatFrequency.Quarter:
                    {
                        var nmonths = (datInput.EndDate.Year - datInput.StartDate.Year) * 12 + (datInput.EndDate.Month - datInput.StartDate.Month);
                        var nquarters = nmonths / 3;

                        for (int i = 0; i <= nquarters; i++)
                        {
                            listResults.Add(new RepeatedDates
                            {
                                StartDate = datInput.StartDate.AddMonths(i * 3),
                            });
                        }

                        for (int i = 0; i < listResults.Count; i++)
                        {
                            if (i == listResults.Count - 1)
                            {
                                listResults[i].EndDate = listResults[i].StartDate.AddMonths(3).AddDays(-1);
                            }
                            else
                            {
                                listResults[i].EndDate = listResults[i + 1].StartDate.AddDays(-1);
                            }
                        }
                    }
                    break;

                case RepeatFrequency.Week:
                    {
                        var tspans = dtEnd - dtStart;
                        var tdays = (Int32)tspans.Days;

                        var tweeks = tdays / 7;

                        for (int i = 0; i <= tweeks; i++)
                        {
                            listResults.Add(new RepeatedDates
                            {
                                StartDate = datInput.StartDate.AddDays(i * 7),
                            });
                        }

                        for (int i = 0; i < listResults.Count; i++)
                        {
                            if (i == listResults.Count - 1)
                            {
                                listResults[i].EndDate = listResults[i].StartDate.AddDays(6);
                            }
                            else
                            {
                                listResults[i].EndDate = listResults[i + 1].StartDate.AddDays(-1);
                            }
                        }
                    }
                    break;

                case RepeatFrequency.Year:
                    {
                        var nyears = datInput.EndDate.Year - datInput.StartDate.Year;

                        for (int i = 0; i <= nyears; i++)
                        {
                            listResults.Add(new RepeatedDates
                            {
                                StartDate = datInput.StartDate.AddYears(i),
                            });
                        }

                        for (int i = 0; i < listResults.Count; i++)
                        {
                            if (i == listResults.Count - 1)
                            {
                                listResults[i].EndDate = listResults[i].StartDate.AddYears(1).AddDays(-1);
                            }
                            else
                            {
                                listResults[i].EndDate = listResults[i + 1].StartDate.AddDays(-1);
                            }
                        }
                    }
                    break;

                case RepeatFrequency.Manual:
                    {
                        // It shall return only entry out
                        listResults.Add(new RepeatedDates
                        {
                            StartDate = datInput.StartDate,
                            EndDate = datInput.EndDate
                        });
                    }
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            return listResults;
        }
 
        public static List<RepeatedDatesWithAmount> WorkoutRepeatedDatesWithAmount(RepeatDatesWithAmountCalculationInput datInput)
        {
            List<RepeatedDatesWithAmount> listResults = new List<RepeatedDatesWithAmount>();

            // Input checks
            if (datInput == null)
                throw new Exception("Input the data!");
            var dtEnd = new DateTime(datInput.EndDate.Year, datInput.EndDate.Month, datInput.EndDate.Day);
            var dtStart = new DateTime(datInput.StartDate.Year, datInput.StartDate.Month, datInput.StartDate.Day);
            if (dtEnd < dtStart)
                throw new Exception("Invalid data range");
            if (datInput.TotalAmount <= 0)
                throw new Exception("Invalid total amount");
            if (String.IsNullOrEmpty(datInput.Desp))
                throw new Exception("Invalid desp");

            switch (datInput.RepeatType)
            {
                case RepeatFrequency.Day:
                    {
                        var tspans = dtEnd - dtStart;
                        var tdays = (Int32)tspans.Days;

                        var tamt = Math.Round(datInput.TotalAmount / tdays, 2);
                        for (int i = 0; i < tdays; i++)
                        {
                            listResults.Add(new RepeatedDatesWithAmount
                            {
                                TranDate = datInput.StartDate.AddDays(i),
                                TranAmount = tamt,
                                Desp = datInput.Desp + " | " + (i + 1).ToString() + " / " + tdays.ToString()
                            });
                        }
                    }
                    break;

                case RepeatFrequency.Fortnight:
                    {
                        var tspans = dtEnd - dtStart;
                        var tdays = (Int32)tspans.Days;

                        var tfortnights = tdays / 14;
                        var tamt = Math.Round(datInput.TotalAmount / tfortnights, 2);

                        for (int i = 0; i < tfortnights; i++)
                        {
                            listResults.Add(new RepeatedDatesWithAmount
                            {
                                TranDate = datInput.StartDate.AddDays(i * 14),
                                TranAmount = tamt,
                                Desp = datInput.Desp + " | " + (i + 1).ToString() + " / " + tfortnights.ToString()
                            });
                        }
                    }
                    break;

                case RepeatFrequency.HalfYear:
                    {
                        var nmonths = (datInput.EndDate.Year - datInput.StartDate.Year) * 12 + (datInput.EndDate.Month - datInput.StartDate.Month);
                        var nhalfyear = nmonths / 6;
                        var tamt = Math.Round(datInput.TotalAmount / nhalfyear, 2);

                        for (int i = 0; i < nhalfyear; i++)
                        {
                            listResults.Add(new RepeatedDatesWithAmount
                            {
                                TranDate = datInput.StartDate.AddMonths(i * 6),
                                TranAmount = tamt,
                                Desp = datInput.Desp + " | " + (i + 1).ToString() + " / " + nhalfyear.ToString()
                            });
                        }
                    }
                    break;

                case RepeatFrequency.Month:
                    {
                        var nmonths = (datInput.EndDate.Year - datInput.StartDate.Year) * 12 + (datInput.EndDate.Month - datInput.StartDate.Month);

                        var tamt = Math.Round(datInput.TotalAmount / nmonths, 2);

                        for (int i = 0; i < nmonths; i++)
                        {
                            listResults.Add(new RepeatedDatesWithAmount
                            {
                                TranDate = datInput.StartDate.AddMonths(i),
                                TranAmount = tamt,
                                Desp = datInput.Desp + " | " + (i + 1).ToString() + " / " + nmonths.ToString()
                            });
                        }
                    }
                    break;

                case RepeatFrequency.Quarter:
                    {
                        var nmonths = (datInput.EndDate.Year - datInput.StartDate.Year) * 12 + (datInput.EndDate.Month - datInput.StartDate.Month);
                        var nquarters = nmonths / 3;
                        var tamt = Math.Round(datInput.TotalAmount / nquarters, 2);

                        for (int i = 0; i < nquarters; i++)
                        {
                            listResults.Add(new RepeatedDatesWithAmount
                            {
                                TranDate = datInput.StartDate.AddMonths(i * 3),
                                TranAmount = tamt,
                                Desp = datInput.Desp + " | " + (i + 1).ToString() + " / " + nquarters.ToString()
                            });
                        }
                    }
                    break;

                case RepeatFrequency.Week:
                    {
                        var tspans = dtEnd - dtStart;
                        var tdays = (Int32)tspans.Days;

                        var tweeks = tdays / 7;
                        var tamt = Math.Round(datInput.TotalAmount / tweeks, 2);

                        for (int i = 0; i < tweeks; i++)
                        {
                            listResults.Add(new RepeatedDatesWithAmount
                            {
                                TranDate = datInput.StartDate.AddDays(i * 7),
                                TranAmount = tamt,
                                Desp = datInput.Desp + " | " + (i + 1).ToString() + " / " + tweeks.ToString()
                            });
                        }
                    }
                    break;

                case RepeatFrequency.Year:
                    {
                        var nyears = datInput.EndDate.Year - datInput.StartDate.Year;

                        var tamt = Math.Round(datInput.TotalAmount / nyears, 2);

                        for (int i = 0; i < nyears; i++)
                        {
                            listResults.Add(new RepeatedDatesWithAmount
                            {
                                TranDate = datInput.StartDate.AddYears(i),
                                TranAmount = tamt,
                                Desp = datInput.Desp + " | " + (i + 1).ToString() + " / " + nyears.ToString()
                            });
                        }
                    }
                    break;

                case RepeatFrequency.Manual:
                    {
                        // It shall return only entry out
                        listResults.Add(new RepeatedDatesWithAmount
                        {
                            TranDate = datInput.EndDate,
                            TranAmount = datInput.TotalAmount,
                            Desp = datInput.Desp + " | 1 / 1"
                        });
                    }
                    break;
            }

            return listResults;
        }
    }
}
