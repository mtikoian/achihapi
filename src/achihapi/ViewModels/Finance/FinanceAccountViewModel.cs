﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace achihapi.ViewModels
{
    public enum FinanceAccountStatus : Byte
    {
        Normal = 0,
        Closed = 1,
        Frozen = 2
    }

    public class FinanceAccountViewModel : BaseViewModel
    {
        public Int32 ID { get; set; }
        [Required]
        public Int32 HID { get; set; }
        [Required]
        public Int32 CtgyID { get; set; }
        [StringLength(30)]
        public String Name { get; set; }
        [StringLength(45)]
        public String Comment { get; set; }
        [StringLength(40)]
        public String Owner { get; set; }
        public FinanceAccountStatus Status { get; set; }

        // Ext ADP
        public FinanceAccountExtDPViewModel ExtraInfo_ADP { get; set; }
        // Ext Asset
        public FinanceAccountExtASViewModel ExtraInfo_AS { get; set; }
        // Ext Loan
        public FinanceAccountExtLoanViewModel ExtraInfo_Loan { get; set; }

        public Boolean IsValid()
        {
            if (String.IsNullOrEmpty(Name))
                return false;
            if (CtgyID <= 0)
                return false;
            if (CtgyID == FinanceAccountCtgyViewModel.AccountCategory_AdvancePayment || CtgyID == FinanceAccountCtgyViewModel.AccountCategory_AdvanceReceive)
            {
                if (ExtraInfo_ADP == null)
                    return false;
                if (!ExtraInfo_ADP.IsValid())
                    return false;
            }
            if (CtgyID == FinanceAccountCtgyViewModel.AccountCategory_Asset)
            {
                if (ExtraInfo_AS == null)
                    return false;
                if (!ExtraInfo_AS.IsValid())
                    return false;
            }
            if (CtgyID == FinanceAccountCtgyViewModel.AccountCategory_BorrowFrom || CtgyID == FinanceAccountCtgyViewModel.AccountCategory_LendTo)
            {
                if (ExtraInfo_Loan == null)
                    return false;
                if (!ExtraInfo_Loan.IsValid())
                    return false;
            }
            return true;
        }

        public static Dictionary<String, Object> WorkoutDeltaForUpdate(FinanceAccountViewModel oldAcnt, 
            FinanceAccountViewModel newAcnt,
            String usrName)
        {
            Dictionary<String, Object> dictDelta = new Dictionary<string, Object>();
            if (oldAcnt == null || newAcnt == null || Object.ReferenceEquals(oldAcnt, newAcnt)
                || oldAcnt.ID != newAcnt.ID || oldAcnt.HID != newAcnt.HID || oldAcnt.CtgyID != newAcnt.CtgyID)
            {
                throw new ArgumentException("Invalid inputted parameter Or ID/HID/Category change is not allowed");
            }
            if (!oldAcnt.IsValid() || !newAcnt.IsValid())
            {
                throw new Exception("Account is invalid");
            }

            // Header
            Type t = typeof(FinanceAccountViewModel);
            Type parent = typeof(BaseViewModel);
            PropertyInfo[] parentProperties = parent.GetProperties();
            Dictionary<String, Object> dictParentProperties = new Dictionary<string, object>();
            foreach (var prop in parentProperties)
                dictParentProperties.Add(prop.Name, null);

            PropertyInfo[] listProperties = t.GetProperties();
            var listSortedProperties = listProperties.OrderBy(o => o.Name);

            foreach (PropertyInfo item in listSortedProperties)
            {
                if (dictParentProperties.ContainsKey(item.Name) 
                    || item.Name == "ID" || item.Name == "HID"
                    || item.Name.StartsWith("ExtraInfo_"))
                {
                    continue;
                }

                object oldValue = item.GetValue(oldAcnt, null);
                object newValue = item.GetValue(newAcnt, null);

                if (item.PropertyType == typeof(Decimal))
                {
                    if (Decimal.Compare((Decimal)oldValue, (Decimal)newValue) != 0) dictDelta.Add(item.Name, newValue);
                }
                else if (item.PropertyType == typeof(String))
                {
                    if (String.CompareOrdinal((string)oldValue, (string)newValue) != 0) dictDelta.Add(item.Name, newValue);
                }
                else if (item.PropertyType == typeof(DateTime))
                {
                    if (DateTime.Compare(((DateTime)oldValue).Date, ((DateTime)newValue).Date) != 0) dictDelta.Add(item.Name, newValue);
                }
                else if (item.PropertyType == typeof(FinanceAccountStatus))
                {
                    if ((FinanceAccountStatus)oldValue != (FinanceAccountStatus)newValue)
                        dictDelta.Add(item.Name, (Byte)newValue);
                }
                else
                {
                    if (!Object.Equals(oldValue, newValue))
                        dictDelta.Add(item.Name, newValue);
                }
            }
            if (dictDelta.Count > 0)
            {
                dictDelta.Add("UpdatedAt", DateTime.Today);
                dictDelta.Add("UpdatedBy", usrName);
            }
            return dictDelta;
        }

        public static string WorkoutDeltaStringForUpdate(FinanceAccountViewModel oldAcnt, 
            FinanceAccountViewModel newAcnt,
            String usrName)
        {
            var diffs = WorkoutDeltaForUpdate(oldAcnt, newAcnt, usrName);

            List<String> listHeaderSqls = new List<string>();
            foreach (var diff in diffs)
            {
                if (diff.Value is DateTime)
                    listHeaderSqls.Add("[" + diff.Key.ToString() + "] = '" + ((DateTime)diff.Value).ToString("yyyy-MM-dd") + "'");
                else if (diff.Value is Boolean)
                    listHeaderSqls.Add("[" + diff.Key.ToString() + "] = " + (((Boolean)diff.Value) ? "1" : "NULL"));
                else if (diff.Value is String)
                {
                    if (String.IsNullOrEmpty((string)diff.Value) && diff.Key == "TranCurr2")
                        listHeaderSqls.Add("[" + diff.Key.ToString() + "] = NULL");
                    else
                        listHeaderSqls.Add("[" + diff.Key.ToString() + "] = N'" + diff.Value + "'");
                }
                else if (diff.Value is Decimal)
                {
                    if (Decimal.Compare((Decimal)diff.Value, 0) == 0)
                    {
                        listHeaderSqls.Add("[" + diff.Key.ToString() + "] = NULL");
                    }
                    else
                        listHeaderSqls.Add("[" + diff.Key.ToString() + "] = " + diff.Value.ToString());
                }
                else
                    listHeaderSqls.Add("[" + diff.Key.ToString() + "] = " + diff.Value.ToString());
            }

            return listHeaderSqls.Count == 0 ?
                String.Empty :
                (@"UPDATE [dbo].[t_fin_account] SET " + string.Join(",", listHeaderSqls) + " WHERE [ID] = " + oldAcnt.ID.ToString());
        }
    }

    public class FinanceAccountUIViewModel : FinanceAccountViewModel
    {
        public string CtgyName { get; set; }
    }

    public abstract class FinanceAccountExtViewModel
    {
        public Int32 AccountID { get; set; }
        public abstract bool IsValid();
    }

    public enum RepeatFrequency : Byte
    {
        Month       = 0,
        Fortnight   = 1,
        Week        = 2,
        Day         = 3,
        Quarter     = 4,
        HalfYear    = 5,
        Year        = 6,
        Manual      = 7,
    }

    // Account extra: advance payment
    public sealed class FinanceAccountExtDPViewModel: FinanceAccountExtViewModel
    {
        public Boolean Direct { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        public RepeatFrequency RptType { get; set; }
        public Int32 RefDocID { get; set; }
        [StringLength(100)]
        public String DefrrDays { get; set; }
        [StringLength(45)]
        public String Comment { get; set; }
        // Tmp. docs
        public List<FinanceTmpDocDPViewModel> DPTmpDocs { get; }

        public FinanceAccountExtDPViewModel()
        {
            this.DPTmpDocs = new List<FinanceTmpDocDPViewModel>();
        }
        public override Boolean IsValid()
        {
            if (EndDate.Date.CompareTo(StartDate.Date) <= 0)
                return false;
            if (String.IsNullOrEmpty(Comment))
                return false;
            if (DPTmpDocs.Count <= 0)
                return false;

            return true;
        }
        public static Dictionary<String, Object> WorkoutDeltaForUpdate(FinanceAccountExtDPViewModel oldAcnt,
            FinanceAccountExtDPViewModel newAcnt)
        {
            Dictionary<String, Object> dictDelta = new Dictionary<string, object>();

            Type t = typeof(FinanceAccountExtDPViewModel);
            PropertyInfo[] listProperties = t.GetProperties();
            var listSortedProperties = listProperties.OrderBy(o => o.Name);

            foreach (PropertyInfo item in listSortedProperties)
            {
                if (item.Name == "Direct" || item.Name == "DefrrDays" || item.Name == "DPTmpDocs")
                {
                    continue;
                }

                object oldValue = item.GetValue(oldAcnt, null);
                object newValue = item.GetValue(newAcnt, null);
                if (item.PropertyType == typeof(Decimal))
                {
                    if (Decimal.Compare((Decimal)oldValue, (Decimal)newValue) != 0) dictDelta.Add(item.Name, newValue);
                }
                else if (item.PropertyType == typeof(String))
                {
                    if (String.CompareOrdinal((string)oldValue, (string)newValue) != 0) dictDelta.Add(item.Name, newValue);
                }
                else if (item.PropertyType == typeof(DateTime))
                {
                    if (DateTime.Compare(((DateTime)oldValue).Date, ((DateTime)newValue).Date) != 0) dictDelta.Add(item.Name, newValue);
                }
                else if(item.PropertyType == typeof(RepeatFrequency))
                {
                    if ((RepeatFrequency)oldValue != (RepeatFrequency)newValue) dictDelta.Add(item.Name, (Byte)newValue);
                }
                else
                {
                    if (!Object.Equals(oldValue, newValue))
                        dictDelta.Add(item.Name, newValue);
                }
            }

            return dictDelta;
        }
    }

    // Account extra: Assert
    public sealed class FinanceAccountExtASViewModel: FinanceAccountExtViewModel
    {
        public Int32 CategoryID { get; set; }
        [StringLength(50)]
        [Required]
        public String Name { get; set; }
        [StringLength(50)]
        public String Comment { get; set; }
        [Required]
        public Int32 RefDocForBuy { get; set; }
        public Int32? RefDocForSold { get; set; }

        public FinanceAccountExtASViewModel(): base()
        {
        }

        public override bool IsValid()
        {
            if (String.IsNullOrEmpty(Name))
                return false;
            if (RefDocForBuy <= 0)
                return false;
            if (CategoryID <= 0)
                return false;

            return true;
        }
        public static Dictionary<String, Object> WorkoutDeltaForUpdate(FinanceAccountExtASViewModel oldAcnt,
            FinanceAccountExtASViewModel newAcnt)
        {
            Dictionary<String, Object> dictDelta = new Dictionary<string, object>();

            Type t = typeof(FinanceAccountExtASViewModel);
            PropertyInfo[] listProperties = t.GetProperties();
            var listSortedProperties = listProperties.OrderBy(o => o.Name);

            foreach (PropertyInfo item in listSortedProperties)
            {
                if (item.Name == "CategoryID")
                {
                    if (oldAcnt.CategoryID != newAcnt.CategoryID)
                    {
                        dictDelta.Add("CTGYID", newAcnt.CategoryID);
                    }
                }
                else if (item.Name == "RefDocForBuy")
                {
                    if (oldAcnt.RefDocForBuy != newAcnt.RefDocForBuy)
                    {
                        dictDelta.Add("REFDOC_BUY", newAcnt.RefDocForBuy);
                    }
                }
                else if (item.Name == "RefDocForSold")
                {
                    if (oldAcnt.RefDocForSold.HasValue)
                    {
                        if (newAcnt.RefDocForSold.HasValue)
                        {
                            if (oldAcnt.RefDocForSold.Value != newAcnt.RefDocForSold.Value)
                            {
                                dictDelta.Add("REFDOC_SOLD", newAcnt.RefDocForSold.Value);
                            }
                        }
                        else
                        {
                            dictDelta.Add("REFDOC_SOLD", null);
                        }
                    }
                    else
                    {
                        if (newAcnt.RefDocForSold.HasValue)
                        {
                            dictDelta.Add("REFDOC_SOLD", newAcnt.RefDocForSold.Value);
                        }
                    }
                }
                else
                {
                    object oldValue = item.GetValue(oldAcnt, null);
                    object newValue = item.GetValue(newAcnt, null);
                    if (item.PropertyType == typeof(Decimal))
                    {
                        if (Decimal.Compare((Decimal)oldValue, (Decimal)newValue) != 0) dictDelta.Add(item.Name, newValue);
                    }
                    else if (item.PropertyType == typeof(String))
                    {
                        if (String.CompareOrdinal((string)oldValue, (string)newValue) != 0) dictDelta.Add(item.Name, newValue);
                    }
                    else if (item.PropertyType == typeof(DateTime))
                    {
                        if (DateTime.Compare(((DateTime)oldValue).Date, ((DateTime)newValue).Date) != 0) dictDelta.Add(item.Name, newValue);
                    }
                    else
                    {
                        if (!Object.Equals(oldValue, newValue))
                            dictDelta.Add(item.Name, newValue);
                    }
                }
            }

            return dictDelta;
        }
    }

    public enum LoanRepaymentMethod
    {
        EqualPrincipalAndInterset   = 1,  // Equal principal & interest
        EqualPrincipal              = 2,  // Equal principal
        DueRepayment                = 3  // Due repayment
    }

    // Account extra: Loan (Borrow from, or Lend to)
    public sealed class FinanceAccountExtLoanViewModel: FinanceAccountExtViewModel
    {
        [Required]
        public DateTime StartDate { get; set; }
        public Decimal? AnnualRate { get; set; }
        public Boolean? InterestFree { get; set; }
        public LoanRepaymentMethod? RepaymentMethod { get; set; }
        public Int16? TotalMonths { get; set; }
        [Required]
        public Int32 RefDocID { get; set; }
        [StringLength(100)]
        public String Others { get; set; }
        public DateTime? EndDate { get; set; }
        //[Required]
        //public Boolean IsLendOut { get; set; }
        public Int32? PayingAccount { get; set; }
        [StringLength(50)]
        public String Partner { get; set; }
        // Tmp. docs
        public List<FinanceTmpDocLoanViewModel> LoanTmpDocs { get; }

        public FinanceAccountExtLoanViewModel()
        {
            //// Default
            //this.IsLendOut = false;
            this.LoanTmpDocs = new List<FinanceTmpDocLoanViewModel>();
        }
        public override bool IsValid()
        {
            if (RefDocID <= 0)
                return false;
            if (LoanTmpDocs.Count < 0)
                return false;

            return true;
        }

        public static Dictionary<String, Object> WorkoutDeltaForUpdate(FinanceAccountExtLoanViewModel oldAcnt,
            FinanceAccountExtLoanViewModel newAcnt)
        {
            Dictionary<String, Object> dictDelta = new Dictionary<string, object>();
            return dictDelta;
        }
    }

    // Account extra: Creditcard
    public sealed class FinanceAccountExtCCViewModel : FinanceAccountExtViewModel
    {
        [Required]
        public Int16 BillDayInMonth { get; set; }
        [Required] 
        public Int16 LastPayDayInMonth { get; set; }
        [Required]
        [StringLength(20)]
        public String CardNumber { get; set; }
        [StringLength(50)]
        public String Bank { get; set; }
        [StringLength(100)]
        public String Others { get; set; }
        public DateTime? ValidDate { get; set; }

        public FinanceAccountExtCCViewModel()
        {
        }

        public override bool IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
