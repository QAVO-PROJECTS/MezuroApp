using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Addresses;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Auth.Adress;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MezuroApp.Persistance.Concretes.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressReadRepository _readRepo;
        private readonly IAddressWriteRepository _writeRepo;
        private readonly IMapper _mapper;

        public AddressService(
            IAddressReadRepository readRepo,
            IAddressWriteRepository writeRepo,
            IMapper mapper)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
            _mapper = mapper;
        }

        #region Queries

        public async Task<List<AdressDto>> GetAllAddressesAsync(string userId)
        {
            var uid = ParseGuidOrThrow(userId, "UserId");

            var addresses = await _readRepo.GetAllAsync(
                x => !x.IsDeleted && x.UserId == uid,
                q => q.Include(x => x.User)
            );

            return _mapper.Map<List<AdressDto>>(addresses);
        }

        public async Task<AdressDto> GetAddressByIdAsync(string userId, string id)
        {
            var uid = ParseGuidOrThrow(userId, "UserId");
            var gid = ParseGuidOrThrow(id, "Id");

            var address = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted && x.UserId == uid,
                q => q.Include(x => x.User)
                
            ) ?? throw new GlobalAppException("ADDRESS_NOT_FOUND");

            return _mapper.Map<AdressDto>(address);
        }

        #endregion

        #region Commands

        public async Task CreateAddressAsync(string userId, CreateAddressDto dto)
        {
            var uid = ParseGuidOrThrow(userId, "UserId");

            var entity = _mapper.Map<UserAddress>(dto);
            entity.Id = Guid.NewGuid();
            entity.UserId = uid;
            entity.CreatedDate = UtcNow();
            entity.LastUpdatedDate = UtcNow();
            entity.IsDeleted = false;

            await _writeRepo.AddAsync(entity);
            await _writeRepo.CommitAsync();
        }

        public async Task UpdateAddressAsync(string userId, UpdateAdressDto dto)
        {
            var uid = ParseGuidOrThrow(userId, "UserId");
            var gid = ParseGuidOrThrow(dto.Id, "Id");

            var address = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted && x.UserId == uid
            ) ?? throw new GlobalAppException("ADDRESS_NOT_FOUND");

            _mapper.Map(dto, address);
            address.LastUpdatedDate = UtcNow();

            await _writeRepo.UpdateAsync(address);
            await _writeRepo.CommitAsync();
        }

        public async Task DeleteAddressAsync(string userId, string id)
        {
            var uid = ParseGuidOrThrow(userId, "UserId");
            var gid = ParseGuidOrThrow(id, "Id");

            var address = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted && x.UserId == uid
        ) ?? throw new GlobalAppException("ADDRESS_NOT_FOUND");

            address.IsDeleted = true;
            address.DeletedDate = UtcNow();
            address.LastUpdatedDate = UtcNow();

            await _writeRepo.UpdateAsync(address);
            await _writeRepo.CommitAsync();
        }

        #endregion

        #region Helpers

        private static Guid ParseGuidOrThrow(string id, string field)
        {
            if (!Guid.TryParse(id, out var gid))
                throw new GlobalAppException("INVALID_ID_FORMAT");
            return gid;
        }

        private static DateTime UtcNow() => DateTime.UtcNow;

        #endregion
    }
}
