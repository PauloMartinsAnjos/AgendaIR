/**
 * CALENDÁRIO DE DISPONIBILIDADE
 * Exibe calendário visual + lista de horários disponíveis/ocupados
 */

// Estado global do calendário
let calendarioState = {
    funcionarioId: null,        // ID do funcionário selecionado
    duracao: 60,                // Duração em minutos (padrão 60)
    dataSelecionada: null,      // Data selecionada no calendário
    mesAtual: new Date(),       // Mês sendo exibido
    ignorarAgendamentoId: null  // ID do agendamento a ignorar (para edição)
};

// Inicializar quando DOM carregar
document.addEventListener('DOMContentLoaded', () => {
    const calendarioElement = document.getElementById('calendario');
    if (calendarioElement) {
        renderizarCalendario(calendarioState.mesAtual);
    }
});

/**
 * Evento: Funcionário foi selecionado
 */
function onFuncionarioChange(funcionarioId) {
    console.log('👤 Funcionário selecionado:', funcionarioId);
    
    calendarioState.funcionarioId = parseInt(funcionarioId);
    calendarioState.dataSelecionada = null;
    
    // Limpar seleção de horários
    const horariosContainer = document.getElementById('horarios-disponiveis');
    if (horariosContainer) {
        horariosContainer.innerHTML = '<div class="alert-rir alert-rir-info">📅 Selecione um dia no calendário</div>';
    }
    
    // Limpar confirmação
    const confirmacao = document.getElementById('confirmacao-horario');
    if (confirmacao) {
        confirmacao.innerHTML = '';
    }
}

/**
 * Renderizar calendário do mês
 */
function renderizarCalendario(data) {
    const calendario = document.getElementById('calendario');
    if (!calendario) return;

    const mesAno = data.toLocaleDateString('pt-BR', { 
        month: 'long', 
        year: 'numeric' 
    });
    
    let html = `
        <div class="calendario-header">
            <button type="button" onclick="mesAnterior()" class="btn-nav">◀</button>
            <h6>${mesAno}</h6>
            <button type="button" onclick="proximoMes()" class="btn-nav">▶</button>
        </div>
        <div class="calendario-grid">
            <div class="dia-semana">DOM</div>
            <div class="dia-semana">SEG</div>
            <div class="dia-semana">TER</div>
            <div class="dia-semana">QUA</div>
            <div class="dia-semana">QUI</div>
            <div class="dia-semana">SEX</div>
            <div class="dia-semana">SÁB</div>
    `;

    // Calcular dias do mês
    const primeiroDia = new Date(data.getFullYear(), data.getMonth(), 1);
    const ultimoDia = new Date(data.getFullYear(), data.getMonth() + 1, 0);
    const diaInicial = primeiroDia.getDay();
    
    const hoje = new Date();
    hoje.setHours(0, 0, 0, 0);

    // Espaços vazios antes do primeiro dia
    for (let i = 0; i < diaInicial; i++) {
        html += '<div class="dia-vazio"></div>';
    }

    // Dias do mês
    for (let dia = 1; dia <= ultimoDia.getDate(); dia++) {
        const dataCompleta = new Date(data.getFullYear(), data.getMonth(), dia);
        const isPast = dataCompleta < hoje;
        const isDiaUtil = dataCompleta.getDay() >= 1 && dataCompleta.getDay() <= 5; // Seg a Sex
        
        let classes = 'dia';
        if (isPast) classes += ' passado';
        if (!isDiaUtil) classes += ' fim-semana';
        
        const onclick = (!isPast && isDiaUtil) 
            ? `onclick="selecionarDia('${dataCompleta.toISOString()}', this)"` 
            : '';
        
        html += `<div class="${classes}" ${onclick}>${dia}</div>`;
    }

    html += '</div>';
    calendario.innerHTML = html;
}

/**
 * Selecionar dia no calendário
 */
async function selecionarDia(dataISO, clickedElement) {
    // Validar se funcionário foi selecionado
    if (!calendarioState.funcionarioId) {
        alert('⚠️ Selecione o funcionário responsável primeiro!');
        return;
    }

    const data = new Date(dataISO);
    calendarioState.dataSelecionada = data;

    console.log('📅 Dia selecionado:', data.toLocaleDateString('pt-BR'));

    // Destacar dia selecionado
    document.querySelectorAll('.dia').forEach(el => el.classList.remove('selecionado'));
    // Use the element from the onclick event if available, otherwise try to find it
    const targetElement = clickedElement || event.target;
    if (targetElement) {
        targetElement.classList.add('selecionado');
    }

    // Carregar horários disponíveis
    await carregarHorarios(data);
}

/**
 * Buscar horários disponíveis via API
 */
async function carregarHorarios(data) {
    const container = document.getElementById('horarios-disponiveis');
    container.innerHTML = '<div class="loading">🔄 Carregando horários...</div>';

    try {
        let url = `/api/disponibilidade?funcionarioId=${calendarioState.funcionarioId}&data=${data.toISOString()}&duracao=${calendarioState.duracao}`;
        
        // Adicionar ignorarAgendamentoId se existir (para edição)
        if (calendarioState.ignorarAgendamentoId) {
            url += `&ignorarAgendamentoId=${calendarioState.ignorarAgendamentoId}`;
        }
        
        console.log('🌐 API Request:', url);
        
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const horarios = await response.json();
        
        console.log('✅ Horários recebidos:', horarios.length);
        
        renderizarHorarios(horarios);
        
    } catch (error) {
        console.error('❌ Erro ao carregar horários:', error);
        container.innerHTML = `
            <div class="alert-rir alert-rir-danger">
                ❌ Erro ao carregar horários disponíveis.<br>
                <small>${error.message}</small>
            </div>
        `;
    }
}

/**
 * Renderizar lista de horários
 */
function renderizarHorarios(horarios) {
    const container = document.getElementById('horarios-disponiveis');
    
    if (!horarios || horarios.length === 0) {
        container.innerHTML = '<div class="alert-rir alert-rir-warning">⚠️ Nenhum horário disponível neste dia</div>';
        return;
    }
    
    const dataFormatada = calendarioState.dataSelecionada.toLocaleDateString('pt-BR', {
        weekday: 'short',
        day: '2-digit',
        month: '2-digit'
    });
    
    let html = `
        <div class="horarios-header">
            <h6>⏰ Horários (${dataFormatada})</h6>
        </div>
        <div class="horarios-lista">
    `;
    
    horarios.forEach(horario => {
        const inicio = new Date(horario.inicio).toLocaleTimeString('pt-BR', {
            hour: '2-digit',
            minute: '2-digit'
        });
        const fim = new Date(horario.fim).toLocaleTimeString('pt-BR', {
            hour: '2-digit',
            minute: '2-digit'
        });

        const classe = horario.disponivel ? 'horario-livre' : 'horario-ocupado';
        const icone = horario.disponivel ? '✅' : '❌';
        const status = horario.disponivel ? 'LIVRE' : 'OCUPADO';
        const onclick = horario.disponivel ? `onclick="selecionarHorario('${horario.inicio}', this)"` : '';

        html += `
            <div class="horario-item ${classe}" ${onclick}>
                <span class="horario-icone">${icone}</span>
                <span class="horario-texto">${inicio} - ${fim}</span>
                <span class="horario-status">[${status}]</span>
            </div>
        `;
    });

    html += '</div>';
    container.innerHTML = html;
}

/**
 * Selecionar horário da lista
 */
function selecionarHorario(dataHoraISO, clickedElement) {
    console.log('⏰ Horário selecionado:', dataHoraISO);
    
    // Atualizar campo hidden do formulário
    const inputDataHora = document.getElementById('DataHora');
    if (inputDataHora) {
        inputDataHora.value = dataHoraISO;
    }

    // Destacar horário selecionado
    document.querySelectorAll('.horario-item').forEach(el => {
        el.classList.remove('selecionado');
    });
    
    // Use the element from the onclick event if available, otherwise try to find it
    const targetElement = clickedElement || event.target;
    if (targetElement) {
        const horarioItem = targetElement.closest('.horario-item');
        if (horarioItem) {
            horarioItem.classList.add('selecionado');
        }
    }

    // Mostrar confirmação visual
    const dataHora = new Date(dataHoraISO);
    const textoConfirmacao = dataHora.toLocaleString('pt-BR', {
        weekday: 'long',
        day: '2-digit',
        month: 'long',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
    
    const confirmacaoDiv = document.getElementById('confirmacao-horario');
    if (confirmacaoDiv) {
        confirmacaoDiv.innerHTML = `
            <div class="alert-rir alert-rir-success">
                ✅ Horário selecionado: <strong>${textoConfirmacao}</strong>
            </div>
        `;
    }
}

/**
 * Navegar para mês anterior
 */
function mesAnterior() {
    calendarioState.mesAtual.setMonth(calendarioState.mesAtual.getMonth() - 1);
    renderizarCalendario(calendarioState.mesAtual);
}

/**
 * Navegar para próximo mês
 */
function proximoMes() {
    calendarioState.mesAtual.setMonth(calendarioState.mesAtual.getMonth() + 1);
    renderizarCalendario(calendarioState.mesAtual);
}
