// ==============================================
// UPLOAD DOCUMENTOS - DRAG & DROP
// Gerencia upload de arquivos com drag & drop
// ==============================================

class UploadDocumentos {
    constructor() {
        this.maxFileSize = 10 * 1024 * 1024; // 10MB
        this.acceptedTypes = ['application/pdf', 'image/jpeg', 'image/png', 'image/jpg'];
        this.uploads = new Map(); // Armazena arquivos por documento ID
        
        this.init();
    }
    
    init() {
        this.setupUploadZones();
        this.setupFileInputs();
    }
    
    setupUploadZones() {
        const uploadZones = document.querySelectorAll('.upload-zone');
        
        uploadZones.forEach(zone => {
            // Drag events
            zone.addEventListener('dragover', (e) => this.handleDragOver(e, zone));
            zone.addEventListener('dragleave', (e) => this.handleDragLeave(e, zone));
            zone.addEventListener('drop', (e) => this.handleDrop(e, zone));
            
            // Click event
            zone.addEventListener('click', () => {
                const fileInput = zone.querySelector('input[type="file"]');
                if (fileInput) {
                    fileInput.click();
                }
            });
        });
    }
    
    setupFileInputs() {
        const fileInputs = document.querySelectorAll('input[type="file"]');
        
        fileInputs.forEach(input => {
            input.addEventListener('change', (e) => this.handleFileSelect(e));
        });
    }
    
    handleDragOver(e, zone) {
        e.preventDefault();
        e.stopPropagation();
        zone.classList.add('drag-over');
    }
    
    handleDragLeave(e, zone) {
        e.preventDefault();
        e.stopPropagation();
        zone.classList.remove('drag-over');
    }
    
    handleDrop(e, zone) {
        e.preventDefault();
        e.stopPropagation();
        zone.classList.remove('drag-over');
        
        const files = e.dataTransfer.files;
        const fileInput = zone.querySelector('input[type="file"]');
        
        if (files.length > 0 && fileInput) {
            // Atribuir arquivo ao input
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(files[0]);
            fileInput.files = dataTransfer.files;
            
            // Trigger change event
            const event = new Event('change', { bubbles: true });
            fileInput.dispatchEvent(event);
        }
    }
    
    handleFileSelect(e) {
        const input = e.target;
        const file = input.files[0];
        
        if (!file) return;
        
        // Validar arquivo
        const validation = this.validateFile(file);
        if (!validation.isValid) {
            this.showError(validation.error, input);
            input.value = ''; // Limpar input
            return;
        }
        
        // Mostrar preview
        this.showPreview(file, input);
        
        // Armazenar arquivo
        const docId = input.dataset.docId;
        if (docId) {
            this.uploads.set(docId, file);
        }
        
        // Atualizar progresso
        this.updateProgress();
        
        // Validar step se estiver usando stepper
        if (window.stepperAgendamento) {
            window.stepperAgendamento.validateStep(4);
        }
    }
    
    validateFile(file) {
        // Validar tipo
        if (!this.acceptedTypes.includes(file.type)) {
            return {
                isValid: false,
                error: 'Tipo de arquivo não permitido. Use PDF, JPG ou PNG.'
            };
        }
        
        // Validar tamanho
        if (file.size > this.maxFileSize) {
            return {
                isValid: false,
                error: 'Arquivo muito grande. Tamanho máximo: 10MB.'
            };
        }
        
        return { isValid: true };
    }
    
    showPreview(file, input) {
        const container = input.closest('.upload-zone, .card-cliente');
        if (!container) return;
        
        // Remover preview anterior
        const oldPreview = container.querySelector('.upload-preview');
        if (oldPreview) {
            oldPreview.remove();
        }
        
        // Criar novo preview
        const preview = document.createElement('div');
        preview.className = 'upload-preview';
        
        const fileSize = this.formatFileSize(file.size);
        const fileIcon = this.getFileIcon(file.type);
        
        preview.innerHTML = `
            <div class="upload-preview-icon">
                <i class="bi bi-${fileIcon}"></i>
            </div>
            <div class="upload-preview-info">
                <div class="upload-preview-name">${file.name}</div>
                <div class="upload-preview-size">${fileSize}</div>
            </div>
            <button type="button" class="upload-preview-remove" title="Remover arquivo">
                <i class="bi bi-x"></i>
            </button>
        `;
        
        // Event listener para remover
        const removeBtn = preview.querySelector('.upload-preview-remove');
        removeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.removeFile(input);
            preview.remove();
        });
        
        // Adicionar preview ao container
        const uploadZoneText = container.querySelector('.upload-zone-text');
        if (uploadZoneText) {
            uploadZoneText.after(preview);
        } else {
            container.appendChild(preview);
        }
    }
    
    removeFile(input) {
        input.value = '';
        
        const docId = input.dataset.docId;
        if (docId) {
            this.uploads.delete(docId);
        }
        
        this.updateProgress();
        
        // Validar step se estiver usando stepper
        if (window.stepperAgendamento) {
            window.stepperAgendamento.validateStep(4);
        }
    }
    
    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }
    
    getFileIcon(type) {
        if (type === 'application/pdf') return 'file-pdf';
        if (type.startsWith('image/')) return 'file-image';
        return 'file-earmark';
    }
    
    updateProgress() {
        const totalDocs = document.querySelectorAll('[data-doc-required="true"]').length;
        const uploadedDocs = this.uploads.size;
        
        const progressBar = document.querySelector('.progress-bar');
        const progressText = document.querySelector('.progress-text');
        
        if (progressBar) {
            const percentage = totalDocs > 0 ? (uploadedDocs / totalDocs) * 100 : 0;
            progressBar.style.width = `${percentage}%`;
        }
        
        if (progressText) {
            progressText.innerHTML = `
                <span>${uploadedDocs} de ${totalDocs} documentos enviados</span>
                <span><strong>${Math.round((uploadedDocs / totalDocs) * 100)}%</strong></span>
            `;
        }
    }
    
    showError(message, input) {
        const container = input.closest('.upload-zone, .card-cliente');
        if (!container) return;
        
        // Criar alerta
        const alert = document.createElement('div');
        alert.className = 'alert alert-danger mt-2';
        alert.innerHTML = `<i class="bi bi-exclamation-triangle"></i> ${message}`;
        
        container.appendChild(alert);
        
        // Remover após 5 segundos
        setTimeout(() => {
            alert.remove();
        }, 5000);
    }
    
    // Método para validar todos os documentos obrigatórios
    validateAllRequired() {
        const requiredInputs = document.querySelectorAll('[data-doc-required="true"]');
        let allValid = true;
        
        requiredInputs.forEach(input => {
            if (!input.files || input.files.length === 0) {
                allValid = false;
            }
        });
        
        return allValid;
    }
    
    // Método para obter todos os arquivos
    getAllFiles() {
        return Array.from(this.uploads.values());
    }
}

// Inicializar quando o DOM estiver pronto
document.addEventListener('DOMContentLoaded', function() {
    // Verificar se há upload zones na página
    if (document.querySelector('.upload-zone, input[type="file"]')) {
        window.uploadDocumentos = new UploadDocumentos();
    }
});

// Prevenir comportamento padrão de drag & drop na página inteira
window.addEventListener('dragover', (e) => {
    e.preventDefault();
}, false);

window.addEventListener('drop', (e) => {
    e.preventDefault();
}, false);
