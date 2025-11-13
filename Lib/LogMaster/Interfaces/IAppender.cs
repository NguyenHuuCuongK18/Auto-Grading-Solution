using LogMaster.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogMaster
{
    public interface IAppender
    {
        void Append(string message);

       
    }
}
