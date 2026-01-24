// ==============================================
// CLIENTE AGENDAMENTO - STEPPER FLOW
// Gerencia fluxo de 5 passos para agendamento
// ==============================================

class StepperAgendamento {
    constructor() {
        this.currentStep = 1;
        this.totalSteps = 5;
        this.data = {
            tipoAgendamento: null,
            dataHora: null,
            documentos: [],
            observacoes: ''
        };
        
        this.init();
    }
    
    init() {
        this.updateStepperUI();
        this.attachEventListeners();
        this.showStep(1);
    }
    
    attachEventListeners() {
        // Botões de navegação
        document.querySelectorAll('[data-step-next]').forEach(btn => {
            btn.addEventListener('click', () => this.nextStep());
        });
        
        document.querySelectorAll('[data-step-prev]').forEach(btn => {
            btn.addEventListener('click', () => this.prevStep());
        });
        
        // Seleção de tipo de agendamento
        const tipoSelect = document.getElementById('tipoAgendamentoSelect');
        if (tipoSelect) {
            tipoSelect.addEventListener('change', (e) => {
                this.data.tipoAgendamento = e.target.value;
                this.validateStep(2);
            });
        }
    }
    
    nextStep() {
        if (!this.validateStep(this.currentStep)) {
            return;
        }
        
        if (this.currentStep < this.totalSteps) {
            this.currentStep++;
            this.showStep(this.currentStep);
            this.updateStepperUI();
        }
    }
    
    prevStep() {
        if (this.currentStep > 1) {
            this.currentStep--;
            this.showStep(this.currentStep);
            this.updateStepperUI();
        }
    }
    
    showStep(step) {
        // Esconder todos os steps
        document.querySelectorAll('.step-content').forEach(content => {
            content.classList.remove('active');
        });
        
        // Mostrar step atual
        const currentContent = document.querySelector(`[data-step="${step}"]`);
        if (currentContent) {
            currentContent.classList.add('active');
        }
        
        // Scroll to top
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
    
    updateStepperUI() {
        // Atualizar círculos
        document.querySelectorAll('.step').forEach((step, index) => {
            const stepNumber = index + 1;
            step.classList.remove('active', 'completed');
            
            if (stepNumber === this.currentStep) {
                step.classList.add('active');
            } else if (stepNumber < this.currentStep) {
                step.classList.add('completed');
            }
        });
        
        // Atualizar barra de progresso
        const progress = ((this.currentStep - 1) / (this.totalSteps - 1)) * 100;
        const progressBar = document.querySelector('.stepper-progress');
        if (progressBar) {
            progressBar.style.width = `${progress}%`;
        }
        
        // Atualizar botões
        this.updateButtons();
    }
    
    updateButtons() {
        const prevBtns = document.querySelectorAll('[data-step-prev]');
        const nextBtns = document.querySelectorAll('[data-step-next]');
        
        // Botão voltar
        prevBtns.forEach(btn => {
            btn.style.display = this.currentStep === 1 ? 'none' : 'inline-flex';
        });
        
        // Botão próximo/finalizar
        nextBtns.forEach(btn => {
            if (this.currentStep === this.totalSteps) {
                btn.textContent = 'Finalizar Agendamento';
                btn.innerHTML = '<i class="bi bi-check-circle"></i> Finalizar Agendamento';
            } else {
                btn.textContent = 'Continuar';
                btn.innerHTML = '<i class="bi bi-arrow-right"></i> Continuar';
            }
        });
    }
    
    validateStep(step) {
        let isValid = true;
        let errorMessage = '';
        
        switch (step) {
            case 1: // Boas-vindas
                isValid = true;
                break;
                
            case 2: // Tipo de agendamento
                if (!this.data.tipoAgendamento) {
                    isValid = false;
                    errorMessage = 'Por favor, selecione o tipo de agendamento';
                }
                break;
                
            case 3: // Data e hora
                if (!this.data.dataHora) {
                    isValid = false;
                    errorMessage = 'Por favor, selecione uma data e horário';
                }
                break;
                
            case 4: // Documentos
                const documentosObrigatorios = document.querySelectorAll('[data-doc-required="true"]');
                let faltamDocs = false;
                
                documentosObrigatorios.forEach(input => {
                    if (!input.files || input.files.length === 0) {
                        faltamDocs = true;
                    }
                });
                
                if (faltamDocs) {
                    isValid = false;
                    errorMessage = 'Por favor, envie todos os documentos obrigatórios';
                }
                break;
                
            case 5: // Confirmação
                isValid = true;
                break;
        }
        
        // Atualizar botão continuar
        const nextBtn = document.querySelector(`[data-step="${step}"] [data-step-next]`);
        if (nextBtn) {
            nextBtn.disabled = !isValid;
        }
        
        // Mostrar mensagem de erro se houver
        if (!isValid && errorMessage) {
            this.showError(errorMessage);
        }
        
        return isValid;
    }
    
    showError(message) {
        // Criar ou atualizar alerta
        let alert = document.querySelector('.step-alert-error');
        
        if (!alert) {
            alert = document.createElement('div');
            alert.className = 'alert alert-danger step-alert-error';
            alert.innerHTML = `<i class="bi bi-exclamation-triangle"></i> ${message}`;
            
            const currentContent = document.querySelector(`[data-step="${this.currentStep}"]`);
            if (currentContent) {
                currentContent.insertBefore(alert, currentContent.firstChild);
            }
        } else {
            alert.innerHTML = `<i class="bi bi-exclamation-triangle"></i> ${message}`;
        }
        
        // Remover após 5 segundos
        setTimeout(() => {
            alert.remove();
        }, 5000);
    }
    
    showSuccess(message) {
        const alert = document.createElement('div');
        alert.className = 'alert alert-success';
        alert.innerHTML = `<i class="bi bi-check-circle"></i> ${message}`;
        
        const currentContent = document.querySelector(`[data-step="${this.currentStep}"]`);
        if (currentContent) {
            currentContent.insertBefore(alert, currentContent.firstChild);
        }
        
        setTimeout(() => {
            alert.remove();
        }, 3000);
    }
    
    // Método para carregar documentos por tipo
    async carregarDocumentosPorTipo(tipoId) {
        try {
            const response = await fetch(`/api/tiposagendamento/${tipoId}/documentos`);
            if (response.ok) {
                const documentos = await response.json();
                this.renderizarDocumentos(documentos);
            } else {
                this.showError('Erro ao carregar documentos. Por favor, tente novamente.');
            }
        } catch (error) {
            console.error('Erro ao carregar documentos:', error);
            this.showError('Erro ao carregar documentos. Verifique sua conexão e tente novamente.');
        }
    }
    
    renderizarDocumentos(documentos) {
        const container = document.getElementById('documentos-container');
        if (!container) return;
        
        container.innerHTML = '';
        
        if (documentos.length === 0) {
            container.innerHTML = '<p class="text-muted">Nenhum documento necessário para este tipo de agendamento.</p>';
            return;
        }
        
        documentos.forEach(doc => {
            const docHtml = `
                <div class="card-cliente mb-3">
                    <div class="d-flex align-items-center justify-content-between">
                        <div>
                            <h5>${doc.nome}</h5>
                            <p class="text-muted mb-0">${doc.descricao || ''}</p>
                            ${doc.obrigatorio ? '<span class="badge-cliente badge-cliente-danger">Obrigatório</span>' : '<span class="badge-cliente badge-cliente-info">Opcional</span>'}
                        </div>
                        <div>
                            <input type="file" 
                                   id="doc-${doc.id}" 
                                   data-doc-id="${doc.id}"
                                   data-doc-required="${doc.obrigatorio}"
                                   accept=".pdf,.jpg,.jpeg,.png"
                                   class="d-none">
                            <button type="button" 
                                    class="btn-cliente-secondary"
                                    onclick="document.getElementById('doc-${doc.id}').click()">
                                <i class="bi bi-upload"></i> Escolher Arquivo
                            </button>
                        </div>
                    </div>
                </div>
            `;
            container.insertAdjacentHTML('beforeend', docHtml);
        });
    }
    
    // Método para submeter o formulário
    async submitForm() {
        if (!this.validateStep(this.totalSteps)) {
            return;
        }
        
        // Aqui você pode adicionar lógica para submeter o formulário
        // Por exemplo, via AJAX ou permitir que o formulário HTML seja submetido
        console.log('Submitting form with data:', this.data);
    }
}

// Inicializar quando o DOM estiver pronto
document.addEventListener('DOMContentLoaded', function() {
    // Verificar se estamos na página de criar agendamento
    if (document.querySelector('.stepper-container')) {
        window.stepperAgendamento = new StepperAgendamento();
    }
});
