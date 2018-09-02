﻿using System;
using System.Collections.Generic;
using achihapi.ViewModels;

namespace achihapi.Utilities
{
    public class FinanceCalcUtility
    {
        public static List<LoanCalcResult> LoanCalculate(LoanCalcViewModel datInput)
        {
            List<LoanCalcResult> listResults = new List<LoanCalcResult>();

            // Input checks
            if (datInput == null)
                throw new Exception("Input the data!");
            if (datInput.InterestFreeLoan && datInput.InterestRate != 0)
                throw new Exception("Cannot input interest rate for Interest-Free loan");
            if (datInput.InterestRate < 0)
                throw new Exception("Interest rate can not be negative");
            if (datInput.TotalAmount <= 0)
                throw new Exception("Total amount must large than zero!");
            if (datInput.RepaymentMethod == LoanRepaymentMethod.EqualPrincipal
                || datInput.RepaymentMethod == LoanRepaymentMethod.EqualPrincipalAndInterset)
            {
                if (datInput.TotalMonths <= 0)
                    throw new Exception("Total months must large than zero");
            }
            else if (datInput.RepaymentMethod == LoanRepaymentMethod.DueRepayment)
            {
                if (!datInput.EndDate.HasValue)
                    throw new Exception("End date must input");
            }
            else
                throw new Exception("Not supported method");
            if (datInput.StartDate == null)
                throw new Exception("Start date is must");
            if(datInput.FirstRepayDate.HasValue && datInput.RepayDayInMonth.HasValue)
            {
                if (datInput.FirstRepayDate.Value.Day != datInput.RepayDayInMonth.Value)
                    throw new Exception("Inconsistency in first payment data and repay day");
            }
            if (datInput.RepayDayInMonth.HasValue)
            {
                if (datInput.RepayDayInMonth.Value <= 0 || datInput.RepayDayInMonth.Value >= 29)
                    throw new Exception("Invalid repay. date");
            }
            if (datInput.FirstRepayDate.HasValue)
            {
                var nInitDays = (int)(datInput.FirstRepayDate.Value.Date - datInput.StartDate.Date).TotalDays;
                // Check the dates
                if (nInitDays < 30 || nInitDays > 60)
                    throw new Exception("First repayment day is invalid");
            }

            var realStartDate = datInput.StartDate;
            if (datInput.FirstRepayDate.HasValue)
                realStartDate = datInput.FirstRepayDate.Value;
            if (datInput.RepayDayInMonth.HasValue && datInput.RepayDayInMonth.Value != realStartDate.Day)
            {
                if (datInput.RepayDayInMonth.Value > realStartDate.Day)
                {
                    realStartDate = realStartDate.AddDays(datInput.RepayDayInMonth.Value - realStartDate.Day);
                }
                else
                {
                    realStartDate = realStartDate.AddMonths(1);
                    realStartDate = realStartDate.AddDays(datInput.RepayDayInMonth.Value - realStartDate.Day);
                }
            }
            var nInitDelay = (int)(realStartDate.Date - datInput.StartDate.Date).TotalDays - 30;

            if (datInput.InterestFreeLoan)
            {
                switch (datInput.RepaymentMethod)
                {
                    case LoanRepaymentMethod.EqualPrincipal:
                    case LoanRepaymentMethod.EqualPrincipalAndInterset:
                        {

                            for (int i = 0; i < datInput.TotalMonths; i++)
                            {
                                listResults.Add(new LoanCalcResult
                                {
                                    TranDate = realStartDate.AddMonths(i),
                                    TranAmount = Math.Round(datInput.TotalAmount / datInput.TotalMonths, 2),
                                    InterestAmount = 0
                                });
                            }
                        }
                        break;

                    case LoanRepaymentMethod.DueRepayment:
                    default:
                        {
                            if (datInput.EndDate.HasValue)
                            {
                                listResults.Add(new LoanCalcResult
                                {
                                    TranDate = datInput.EndDate.Value,
                                    TranAmount = datInput.TotalAmount,
                                    InterestAmount = 0
                                });
                            }
                            else
                            {
                                listResults.Add(new LoanCalcResult
                                {
                                    TranDate = datInput.StartDate,
                                    TranAmount = datInput.TotalAmount,
                                    InterestAmount = 0
                                });
                            }
                        }
                        break;
                }
            }
            else
            {
                // Have interest rate inputted
                switch (datInput.RepaymentMethod)
                {
                    case LoanRepaymentMethod.EqualPrincipalAndInterset:
                        {
                            // Decimal dInitMonthIntere = 0;
                            //每月月供额 =〔贷款本金×月利率×(1＋月利率)＾还款月数〕÷〔(1＋月利率)＾还款月数 - 1〕
                            //每月应还利息 = 贷款本金×月利率×〔(1 + 月利率) ^ 还款月数 - (1 + 月利率) ^ (还款月序号 - 1)〕÷〔(1 + 月利率) ^ 还款月数 - 1〕
                            //每月应还本金 = 贷款本金×月利率×(1 + 月利率) ^ (还款月序号 - 1)÷〔(1 + 月利率) ^ 还款月数 - 1〕
                            Decimal monthRate = datInput.InterestRate / 12;
                            //if (nInitDelay > 0)
                            //    dInitMonthIntere = Math.Round(datInput.TotalAmount * (monthRate / 30) * nInitDelay, 2);
                            Decimal d3 = (Decimal)Math.Pow((double)(1 + monthRate), datInput.TotalMonths) - 1;
                            Decimal monthRepay = datInput.TotalAmount * monthRate * (Decimal)Math.Pow((double)(1 + monthRate), datInput.TotalMonths) / d3;

                            Decimal totalInterestAmt = 0;
                            for (int i = 0; i < datInput.TotalMonths; i++)
                            {
                                var rst = new LoanCalcResult
                                {
                                    TranDate = realStartDate.AddMonths(i),
                                    TranAmount = Math.Round(datInput.TotalAmount * monthRate * (Decimal)Math.Pow((double)(1 + monthRate), i) / d3, 2),
                                    InterestAmount = Math.Round(datInput.TotalAmount * monthRate * ((Decimal)Math.Pow((double)(1 + monthRate), datInput.TotalMonths) - (Decimal)Math.Pow((double)(1 + monthRate), i)) / d3, 2)
                                };

                                if (i == 0 && nInitDelay > 0)
                                    rst.InterestAmount = Math.Round(rst.InterestAmount + (nInitDelay - 1) * datInput.TotalAmount * monthRate / 30, 2);

                                //var diff = rst.TranAmount + rst.InterestAmount - monthRepay;
                                //if (diff != 0)
                                //{
                                //    rst.TranAmount -= diff;
                                //    rst.TranAmount = Math.Round(rst.TranAmount, 2);
                                //}

                                totalInterestAmt += rst.InterestAmount;

                                listResults.Add(rst);
                            }
                        }
                        break;

                    case LoanRepaymentMethod.EqualPrincipal:
                        {
                            // 每月月供额 = (贷款本金÷还款月数) + (贷款本金 - 已归还本金累计额)×月利率
                            // 每月应还本金 = 贷款本金÷还款月数
                            // 每月应还利息 = 剩余本金×月利率 = (贷款本金 - 已归还本金累计额)×月利率
                            // 每月月供递减额 = 每月应还本金×月利率 = 贷款本金÷还款月数×月利率
                            // 总利息 = 还款月数×(总贷款额×月利率 - 月利率×(总贷款额÷还款月数)*(还款月数 - 1)÷2 + 总贷款额÷还款月数)
                            Decimal monthRate = datInput.InterestRate / 12;
                            Decimal totalAmt = datInput.TotalAmount;
                            var monthPrincipal = datInput.TotalAmount / datInput.TotalMonths;

                            for (int i = 0; i < datInput.TotalMonths; i++)
                            {
                                var rst = new LoanCalcResult
                                {
                                    TranDate = realStartDate.AddMonths(i + 1),
                                    TranAmount = Math.Round(monthPrincipal, 2),
                                    InterestAmount = Math.Round(totalAmt * monthRate, 2)
                                };
                                if (i == 0 && nInitDelay > 0)
                                    rst.InterestAmount = Math.Round(rst.InterestAmount + (nInitDelay - 1) * datInput.TotalAmount * monthRate / 30, 2);

                                totalAmt -= monthPrincipal;

                                listResults.Add(rst);
                            }
                        }
                        break;

                    case LoanRepaymentMethod.DueRepayment:
                        {
                            Decimal monthRate = datInput.InterestRate / 12;
                            Decimal amtInterest = 0;
                            if (datInput.EndDate.HasValue)
                            {
                                TimeSpan ts = datInput.EndDate.Value - datInput.StartDate;
                                amtInterest = datInput.TotalAmount * (Int32)Math.Round(ts.TotalDays / 30) * monthRate;
                            }
                            else if (datInput.TotalAmount > 0)
                            {
                                amtInterest = datInput.TotalAmount * datInput.TotalMonths * monthRate;
                            }

                            var rst = new LoanCalcResult
                            {
                                TranDate = datInput.StartDate.AddMonths(datInput.TotalMonths),
                                TranAmount = datInput.TotalAmount,
                                InterestAmount = amtInterest
                            };

                            listResults.Add(rst);
                        }
                        break;

                    default: throw new Exception("Unsupported repayment method");
                }
            }

            return listResults;
        }

        public static List<ADPGenerateResult> GenerateAdvancePaymentTmps(ADPGenerateViewModel datInput)
        {
            List<ADPGenerateResult> listResults = new List<ADPGenerateResult>();

            // Input checks
            if (datInput == null)
                throw new Exception("Input the data!");
            if (datInput.EndDate < datInput.StartDate)
                throw new Exception("Invalid data range");
            if (datInput.TotalAmount <= 0)
                throw new Exception("Invalid total amount");
            if (String.IsNullOrEmpty(datInput.Desp))
                throw new Exception("Invalid desp");

            switch (datInput.RptType)
            {
                case RepeatFrequency.Day:
                    {
                        var tspans = datInput.EndDate.Date - datInput.StartDate.Date;
                        var tdays = (Int32)tspans.Days;

                        var tamt = Math.Round(datInput.TotalAmount / tdays, 2);
                        for (int i = 0; i < tdays; i++)
                        {
                            listResults.Add(new ADPGenerateResult
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
                        var tspans = datInput.EndDate.Date - datInput.StartDate.Date;
                        var tdays = (Int32)tspans.Days;

                        var tfortnights = tdays / 14;
                        var tamt = Math.Round(datInput.TotalAmount / tfortnights, 2);

                        for (int i = 0; i < tfortnights; i++)
                        {
                            listResults.Add(new ADPGenerateResult
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
                            listResults.Add(new ADPGenerateResult
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
                            listResults.Add(new ADPGenerateResult
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
                            listResults.Add(new ADPGenerateResult
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
                        var tspans = datInput.EndDate.Date - datInput.StartDate.Date;
                        var tdays = (Int32)tspans.Days;

                        var tweeks = tdays / 7;
                        var tamt = Math.Round(datInput.TotalAmount / tweeks, 2);

                        for (int i = 0; i < tweeks; i++)
                        {
                            listResults.Add(new ADPGenerateResult
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
                            listResults.Add(new ADPGenerateResult
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
                        // TBD.
                    }
                    break;
            }

            return listResults;
        }
    }
}
