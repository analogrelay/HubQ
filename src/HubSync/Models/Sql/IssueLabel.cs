using System;
using System.Collections.Generic;
using System.Text;

namespace HubSync.Models.Sql
{
    public class IssueLabel
    {
        public int IssueId { get; set; }
        public int LabelId { get; set; }

        public virtual Issue Issue { get; set; }
        public virtual Label Label { get; set; }
    }
}
