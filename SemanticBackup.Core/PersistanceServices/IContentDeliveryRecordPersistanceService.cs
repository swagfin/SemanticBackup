using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IContentDeliveryRecordPersistanceService
    {
        List<ContentDeliveryRecord> GetAll(string resourceGroupId);
        ContentDeliveryRecord GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(ContentDeliveryRecord record);
        bool Update(ContentDeliveryRecord record);
        bool UpdateStatusFeed(string id, string status, string message = null, long executionInMilliseconds = 0);
        List<ContentDeliveryRecord> GetAllByStatus(string status);
        List<ContentDeliveryRecord> GetAllByBackupRecordId(string id);
        List<ContentDeliveryRecord> GetAllByBackupRecordIdByStatus(string resourceGroupId, string id, string status = "*");
        ContentDeliveryRecord GetByContentTypeByExecutionMessage(string deliveryType, string executionMessage);
        List<string> GetAllNoneResponsive(List<string> statusChecks, int minuteDifference);
    }
}
