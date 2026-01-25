// ==============================================
// CLIENTE AGENDAMENTO - STEPPER FLOW
// Gerencia fluxo de 5 passos para agendamento
// ==============================================

class StepperAgendamento {
    constructor() {
        this.currentStep = 1;
        this.totalSteps = 5;
        this.temDocumentos = false; // ✅ NOVO
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
            tipoSelect.addEventListener('change', async (e) => {
                this.data.tipoAgendamento = e.target.value;

                // ✅ NOVO: Carregar documentos do tipo
                if (e.target.value) {
                    await this.carregarDocumentosPorTipo(e.target.value);
                }

                this.validateStep(2);
            });
        }

        // ✅ Escutar evento de horário selecionado
        window.addEventListener('horarioSelecionado', (e) => {
            console.log('🎯 Evento horarioSelecionado capturado pela classe!', e.detail);

            this.data.dataHora = e.detail.dataHoraISO || e.detail.dataHora;

            console.log('✅ DataHora atualizado:', this.data.dataHora);

            this.validateStep(3);
        });
    }

    nextStep() {
        if (!this.validateStep(this.currentStep)) {
            return;
        }

        if (this.currentStep < this.totalSteps) {
            this.currentStep++;

            // ✅ NOVO: Pular passo 4 (documentos) se não houver documentos
            if (this.currentStep === 4 && !this.temDocumentos) {
                console.log('⏩ Pulando passo 4 (sem documentos)');
                this.currentStep = 5;
            }

            this.showStep(this.currentStep);
            this.updateStepperUI();
        }
    }

    prevStep() {
        if (this.currentStep > 1) {
            this.currentStep--;

            // ✅ NOVO: Pular passo 4 ao voltar se não houver documentos
            if (this.currentStep === 4 && !this.temDocumentos) {
                console.log('⏩ Pulando passo 4 ao voltar (sem documentos)');
                this.currentStep = 3;
            }

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

        // Ações específicas por passo
        if (step === 3) {
            const funcionarioIdInput = document.getElementById('FuncionarioId');
            if (funcionarioIdInput && funcionarioIdInput.value) {
                const funcionarioId = parseInt(funcionarioIdInput.value);
                if (typeof calendarioState !== 'undefined') {
                    calendarioState.funcionarioId = funcionarioId;
                    console.log('✅ Calendário inicializado com funcionário:', funcionarioId);

                    if (typeof renderizarCalendario === 'function') {
                        renderizarCalendario(calendarioState.mesAtual);
                    }
                }
            } else {
                console.error('❌ FuncionarioId não encontrado ou vazio');
            }
        }

        // ✅ NOVO: Renderizar resumo no passo 5
        if (step === 5) {
            this.renderizarResumo();
        }

        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    updateStepperUI() {
        document.querySelectorAll('.step').forEach((step, index) => {
            const stepNumber = index + 1;
            step.classList.remove('active', 'completed');

            if (stepNumber === this.currentStep) {
                step.classList.add('active');
            } else if (stepNumber < this.currentStep) {
                step.classList.add('completed');
            }
        });

        const progress = ((this.currentStep - 1) / (this.totalSteps - 1)) * 100;
        const progressBar = document.querySelector('.stepper-progress');
        if (progressBar) {
            progressBar.style.width = `${progress}%`;
        }

        this.updateButtons();
    }

    updateButtons() {
        const prevBtns = document.querySelectorAll('[data-step-prev]');
        const nextBtns = document.querySelectorAll('[data-step-next]');

        prevBtns.forEach(btn => {
            btn.style.display = this.currentStep === 1 ? 'none' : 'inline-flex';
        });

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
            case 1:
                isValid = true;
                break;

            case 2:
                if (!this.data.tipoAgendamento) {
                    isValid = false;
                    errorMessage = 'Por favor, selecione o tipo de agendamento';
                }
                break;

            case 3:
                console.log('🔍 Validando passo 3. DataHora:', this.data.dataHora);

                if (!this.data.dataHora) {
                    isValid = false;
                    errorMessage = 'Por favor, selecione uma data e horário';
                } else {
                    console.log('✅ Passo 3 válido!');
                }
                break;

            case 4:
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

            case 5:
                isValid = true;
                break;
        }

        const currentStepContent = document.querySelector(`[data-step="${step}"]`);
        if (currentStepContent) {
            const nextBtn = currentStepContent.querySelector('[data-step-next]');
            if (nextBtn) {
                nextBtn.disabled = !isValid;

                if (isValid) {
                    console.log(`✅ Botão do passo ${step} habilitado`);
                } else {
                    console.log(`❌ Botão do passo ${step} desabilitado:`, errorMessage);
                }
            }
        }

        if (!isValid && errorMessage && step === this.currentStep) {
            this.showError(errorMessage);
        }

        return isValid;
    }

    showError(message) {
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

    async carregarDocumentosPorTipo(tipoId) {
        try {
            const response = await fetch(`/api/tiposagendamento/${tipoId}/documentos`);

            if (response.ok) {
                const documentos = await response.json();

                this.temDocumentos = documentos && documentos.length > 0;

                console.log(`📋 Documentos encontrados: ${documentos.length}`);
                console.log(`📌 Tem documentos obrigatórios: ${this.temDocumentos}`);

                this.renderizarDocumentos(documentos);
            } else if (response.status === 404) {
                console.log('📋 Nenhum documento encontrado para este tipo');
                this.temDocumentos = false;
                this.renderizarDocumentos([]);
            } else {
                this.showError('Erro ao carregar documentos. Por favor, tente novamente.');
            }
        } catch (error) {
            console.error('Erro ao carregar documentos:', error);
            this.temDocumentos = false;
        }
    }

    renderizarDocumentos(documentos) {
        const container = document.getElementById('documentos-container');
        if (!container) return;

        container.innerHTML = '';

        if (documentos.length === 0) {
            container.innerHTML = '<p class="text-muted">Nenhum documento necessário para este tipo de agendamento.</p>';
            this.validateStep(4);
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
                                   class="d-none"
                                   onchange="window.stepperAgendamento.onDocumentoUpload(this)">
                            <button type="button" 
                                    class="btn-cliente-secondary"
                                    onclick="document.getElementById('doc-${doc.id}').click()">
                                <i class="bi bi-upload"></i> Escolher Arquivo
                            </button>
                        </div>
                    </div>
                    <div id="preview-${doc.id}" class="mt-2"></div>
                </div>
            `;
            container.insertAdjacentHTML('beforeend', docHtml);
        });

        this.validateStep(4);
    }

    onDocumentoUpload(input) {
        if (input.files && input.files[0]) {
            const file = input.files[0];
            const docId = input.dataset.docId;

            const preview = document.getElementById(`preview-${docId}`);
            if (preview) {
                preview.innerHTML = `
                    <div class="alert alert-success">
                        <i class="bi bi-check-circle"></i> ${file.name} (${(file.size / 1024).toFixed(1)} KB)
                    </div>
                `;
            }

            console.log('📎 Documento enviado:', file.name);

            this.validateStep(4);
        }
    }

    renderizarResumo() {
        console.log('📋 Renderizando resumo...');
        console.log('📅 DataHora:', this.data.dataHora);
        
        // Buscar dados do formulário
        const tipoSelect = document.getElementById('tipoAgendamentoSelect');
        const tipoNome = tipoSelect?.options[tipoSelect.selectedIndex]?.text || 'Não selecionado';
        
        // Atualizar tipo no resumo
        const resumoTipo = document.getElementById('resumo-tipo');
        if (resumoTipo) {
            resumoTipo.textContent = tipoNome;
        }
        
        // Atualizar data e hora
        const dataHora = this.data.dataHora ? new Date(this.data.dataHora) : null;
        
        if (dataHora && !isNaN(dataHora.getTime())) {
            // Formatar data
            const dataFormatada = dataHora.toLocaleDateString('pt-BR', {
                weekday: 'long',
                day: '2-digit',
                month: 'long',
                year: 'numeric'
            });
            
            // Formatar horário
            const horaFormatada = dataHora.toLocaleTimeString('pt-BR', {
                hour: '2-digit',
                minute: '2-digit'
            });
            
            console.log('✅ Data formatada:', dataFormatada);
            console.log('✅ Hora formatada:', horaFormatada);
            
            // Atualizar elementos na VIEW usando IDs
            const resumoData = document.getElementById('resumo-data');
            const resumoHora = document.getElementById('resumo-hora');
            
            if (resumoData) {
                resumoData.textContent = dataFormatada;
                console.log('✅ Campo Data atualizado');
            } else {
                console.warn('⚠️ Elemento resumo-data não encontrado');
            }
            
            if (resumoHora) {
                resumoHora.textContent = horaFormatada;
                console.log('✅ Campo Hora atualizado');
            } else {
                console.warn('⚠️ Elemento resumo-hora não encontrado');
            }
        } else {
            console.error('❌ DataHora inválida:', this.data.dataHora);
        }
        
        console.log('✅ Resumo renderizado');
    }

    async submitForm() {
        if (!this.validateStep(this.totalSteps)) {
            return;
        }

        console.log('Submitting form with data:', this.data);
    }
}

document.addEventListener('DOMContentLoaded', function () {
    if (document.querySelector('.stepper-container')) {
        window.stepperAgendamento = new StepperAgendamento();
        console.log('✅ StepperAgendamento inicializado');
    }
});