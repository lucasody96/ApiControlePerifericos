using ApiControlePerifericos.Models;
using AutoMapper;

namespace ApiControlePerifericos.DTOs.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Produto, ProdutoDTO>().ReverseMap();
            CreateMap<Colaborador, ColaboradorDTO>().ReverseMap();
            CreateMap<Movimentacao, MovimentacaoDTO>().ReverseMap();
        }
    }
}
